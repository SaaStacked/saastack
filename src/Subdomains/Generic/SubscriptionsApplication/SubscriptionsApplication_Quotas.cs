using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;
using SubscriptionsApplication.Persistence.ReadModels;
using SubscriptionsDomain;

namespace SubscriptionsApplication;

partial class SubscriptionsApplication
{
#if TESTINGONLY
    public Task<Result<Error>> CheckQuotaAsync(ICallerContext caller, string quotaId, long total,
        CancellationToken cancellationToken)
    {
        return TryCheckQuotaUsageAsync(caller, quotaId, total, cancellationToken);
    }
#endif

    /// <summary>
    ///     Checks, and if permitted, assigns the proposed total for the specified <see cref="quotaId" /> else returns
    ///     <see cref="Error.FeatureViolation" />.
    ///     This method automatically updates existing usages to the current quota definition of the provider.
    /// </summary>
    public async Task<Result<Error>> TryCheckQuotaUsageAsync(ICallerContext caller, string quotaId,
        long proposedTotal, CancellationToken cancellationToken)
    {
        if (!caller.TenantId.HasValue)
        {
            return Error.EntityNotFound();
        }

        var tenantId = caller.TenantId.Value;
        var retrievedOwningEntity =
            await _subscriptionOwningEntityService.GetEntityAsync(caller, tenantId, cancellationToken);
        if (retrievedOwningEntity.IsFailure)
        {
            return retrievedOwningEntity.Error;
        }

        var owningEntityId = retrievedOwningEntity.Value.Id.ToId();
        var retrievedSubscription = await _repository.FindByOwningEntityIdAsync(owningEntityId, cancellationToken);
        if (retrievedSubscription.IsFailure)
        {
            return retrievedSubscription.Error;
        }

        if (!retrievedSubscription.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var subscription = retrievedSubscription.Value.Value;
        if (_billingProvider.Capabilities.QuotaManagement is not ManagementOptions.RequiresManaged)
        {
            return Result.Ok;
        }

        var providerQuotas = _billingProvider.Capabilities.ManagedQuotas;
        if (providerQuotas.NotExists()
            || providerQuotas.HasNone())
        {
            return Result.Ok;
        }

        if (!subscription.ManagedQuotas.HasValue)
        {
            return Result.Ok;
        }

        var subscriptionQuotas = subscription.ManagedQuotas.Value;
        var subscriptionTier = subscriptionQuotas.Tier;
        var tierQuotas = providerQuotas.ForTier(subscriptionTier);
        if (!tierQuotas.HasValue)
        {
            return Result.Ok;
        }

        if (!tierQuotas.Value.Items.TryGetValue(quotaId, out var quotaDefinition))
        {
            return Result.Ok;
        }

        var providerName = subscription.Provider.Value.Name;
        var retrievedUsages = await _quotaRepository.SearchAllByOwningEntityIdAsync(
            providerName, owningEntityId, new SearchOptions(), cancellationToken);
        if (retrievedUsages.IsFailure)
        {
            return retrievedUsages.Error;
        }

        var usages = retrievedUsages.Value;
        SubscriptionQuotaUsageRoot usage;
        var existingUsage =
            usages.Results.FindUsageForSubscriptionAndProvider(subscription, providerName, subscriptionTier, quotaId);
        if (existingUsage.Exists())
        {
            var retrieved =
                await _quotaRepository.LoadAsync(owningEntityId, existingUsage.Id.Value.ToId(), cancellationToken);
            if (retrieved.IsFailure)
            {
                return retrieved.Error;
            }

            usage = retrieved.Value;
        }
        else
        {
            var created =
                SubscriptionQuotaUsageRoot.Create(_recorder, _identifierFactory, subscription.Id, owningEntityId,
                    providerName);
            if (created.IsFailure)
            {
                return created.Error;
            }

            usage = created.Value;
        }

        // Reconfigures iff different
        var usageConfigured = usage.Configure(subscriptionTier, quotaId, quotaDefinition);
        if (usageConfigured.IsFailure)
        {
            return usageConfigured.Error;
        }

        var totaled = usage.SetTotal(proposedTotal);
        if (totaled.IsFailure)
        {
            return totaled.Error;
        }

        var saved = await _quotaRepository.SaveAsync(usage, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        usage = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Subscription {Id} updated quota usage {QuotaId} to {Total}",
            subscription.Id, usage.QuotaId, proposedTotal);

        return Result.Ok;
    }

    /// <summary>
    ///     Reconciles the quota usages for the <see cref="currentTier" /> (for this subscription).
    ///     As we can change tiers at any time, we need to deal with the case where these usages may already exist for a
    ///     previous tier, and/or the current tier already. If they do exist, we need to delete them, and ensure that we
    ///     only have usages for the current tier. If a previous tier already exists, we cannot lose its existing quotas,
    ///     and we must carry them forward, as well as updating from the  current quota definition (in case that has
    ///     also changed since writing the last tier).
    ///     Notes:
    ///     1. When <see cref="previousTier" /> is <see cref="Optional{TValue}.None" />, then we are syncing the subscription
    ///     for the very first time (first time after creation). In this case, there cannot be any existing usages for this
    ///     subscription. Otherwise, we assume there could be existing usages when transitioning from
    ///     <see cref="previousTier" /> to <see cref="currentTier" />, since we can upgrade/downgrade at any time.
    ///     2. When moving from <see cref="previousTier" /> to <see cref="currentTier" />, and a usage for this same quota also
    ///     exists in a previous tier, then we need carry forward its total to current tier quota, and update the quota
    ///     definition of the provider (since that may have changed in the interim).
    ///     3. When moving from <see cref="previousTier" /> to <see cref="currentTier" />, and the tiers are the same,
    ///     then we only recreate the quotas if the quota definitions are any different - no need to rewrite them.
    ///     4. When moving from <see cref="previousTier" /> to <see cref="currentTier" /> it is possible that the carried
    ///     forward total exceed these new limits. In these cases, we simply max out the total at the limit.
    /// </summary>
    public async Task<Result<Error>> ResyncSubscriptionTierQuotasInternalAsync(ICallerContext caller,
        SubscriptionRoot subscription, Optional<BillingSubscriptionTier> previousTier,
        BillingSubscriptionTier currentTier, CancellationToken cancellationToken)
    {
        List<SubscriptionQuotaUsage> cachedPreviousUsages = [];
        var providerName = subscription.Provider.Value.Name;
        var owningEntityId = subscription.OwningEntityId;
        var quotas = _billingProvider.Capabilities.ManagedQuotas.ToOptional();

        if (IsFirstSync())
        {
            if (HasNoQuotaDefinitions())
            {
                return Result.Ok;
            }
        }

        if (!IsFirstSync())
        {
            if (AreTiersTheSame())
            {
                //Are stores quotas and providers quotas any different now?
                var sameTier = currentTier;
                var subscriptionTierQuota = subscription.ManagedQuotas.HasValue
                    ? subscription.ManagedQuotas.Value.Quotas
                    : Optional<ProviderPlanQuotas>.None;
                var providerQuota = quotas.Value.ForTier(sameTier);
                if (subscriptionTierQuota.HasValue
                    && subscriptionTierQuota == providerQuota)
                {
                    return Result.Ok;
                }
            }
        }

        if (!IsFirstSync())
        {
            var retrievedUsages =
                await _quotaRepository.SearchAllByOwningEntityIdAsync(providerName, owningEntityId, new SearchOptions(),
                    cancellationToken);
            if (retrievedUsages.IsFailure)
            {
                return retrievedUsages.Error;
            }

            cachedPreviousUsages = retrievedUsages.Value.Results;
        }

        if (!IsFirstSync())
        {
            if (HasNoQuotaDefinitions())
            {
                if (cachedPreviousUsages.HasAny())
                {
                    return await DeleteAllUsages();
                }

                return Result.Ok;
            }
        }

        var currentTierQuotas = quotas.Value.ForTier(currentTier);
        if (!currentTierQuotas.HasValue)
        {
            if (!IsFirstSync())
            {
                // Nothing to do here except cleanup all old tiers
                return await DeleteAllUsages();
            }

            return Result.Ok;
        }

        if (!IsFirstSync())
        {
            // Cleanup all tiers but previous
            var allButPreviousTierDeleted = await DeleteOtherTierUsages(previousTier.Value);
            if (allButPreviousTierDeleted.IsFailure)
            {
                return allButPreviousTierDeleted.Error;
            }
        }

        // Create usages for current tier
        foreach (var (quotaId, quotaDefinition) in currentTierQuotas.Value.Items)
        {
            // Find usage from previous tier (for same quota) 
            var previousTierUsage = IsFirstSync()
                ? null
                : cachedPreviousUsages.FindUsageForSubscriptionAndProvider(subscription, providerName,
                    previousTier.Value, quotaId);
            if (previousTierUsage.Exists())
            {
                var previousTierTotal = previousTierUsage.Total;
                var updated = await CreateNewUsageAsync(quotaId, previousTierTotal, quotaDefinition);
                if (updated.IsFailure)
                {
                    return updated.Error;
                }
            }
            else
            {
                var created = await CreateNewUsageAsync(quotaId, Optional<long>.None, quotaDefinition);
                if (created.IsFailure)
                {
                    return created.Error;
                }
            }
        }

        if (!IsFirstSync())
        {
            var previousDeleted = await DeleteOnlyTierUsages(previousTier.Value);
            if (previousDeleted.IsFailure)
            {
                return previousDeleted.Error;
            }
        }

        return Result.Ok;

        async Task<Result<Error>> CreateNewUsageAsync(string quotaId, Optional<long> carriedOverTotal,
            ProviderPlanQuota definition)
        {
            var created = SubscriptionQuotaUsageRoot.Create(_recorder, _identifierFactory,
                subscription.Id, subscription.OwningEntityId, providerName);
            if (created.IsFailure)
            {
                return created.Error;
            }

            var usage = created.Value;

            var usageConfigured = usage.Configure(currentTier, quotaId, definition);
            if (usageConfigured.IsFailure)
            {
                return usageConfigured.Error;
            }

            if (carriedOverTotal.HasValue)
            {
                //Potential overflow if moving from a plan with higher totals to lower limits 
                var newTotal = carriedOverTotal.Value;
                if (definition.Limit != -1 &&
                    carriedOverTotal.Value > definition.Limit)
                {
                    newTotal = definition.Limit;
                }

                var totalled = usage.SetTotal(newTotal);
                if (totalled.IsFailure)
                {
                    return totalled.Error;
                }
            }

            var savedUsage = await _quotaRepository.SaveAsync(usage, cancellationToken);
            if (savedUsage.IsFailure)
            {
                return savedUsage.Error;
            }

            usage = savedUsage.Value;
            _recorder.TraceInformation(caller.ToCall(), "Subscription {Id} created quota usage {Usage} for {Tier}",
                subscription.Id, usage.Id, currentTier);

            return Result.Ok;
        }

        async Task<Result<Error>> DeleteOnlyTierUsages(BillingSubscriptionTier includingTier)
        {
            var includedTiers = cachedPreviousUsages.Where(use => use.SubscriptionTier == includingTier);
            foreach (var usage in includedTiers)
            {
                var usageId = usage.Id.Value.ToId();
                var deleted = await _quotaRepository.DeleteUsageAsync(owningEntityId, usageId, cancellationToken);
                if (deleted.IsFailure)
                {
                    return deleted.Error;
                }
            }

            return Result.Ok;
        }

        async Task<Result<Error>> DeleteOtherTierUsages(BillingSubscriptionTier excludingTier)
        {
            var includedTiers = cachedPreviousUsages.Where(use => use.SubscriptionTier != excludingTier);
            foreach (var usage in includedTiers)
            {
                var usageId = usage.Id.Value.ToId();
                var deleted = await _quotaRepository.DeleteUsageAsync(owningEntityId, usageId, cancellationToken);
                if (deleted.IsFailure)
                {
                    return deleted.Error;
                }
            }

            return Result.Ok;
        }

        async Task<Result<Error>> DeleteAllUsages()
        {
            foreach (var usage in cachedPreviousUsages)
            {
                var usageId = usage.Id.Value.ToId();
                var deleted = await _quotaRepository.DeleteUsageAsync(owningEntityId, usageId, cancellationToken);
                if (deleted.IsFailure)
                {
                    return deleted.Error;
                }
            }

            return Result.Ok;
        }

        bool HasNoQuotaDefinitions()
        {
            return !quotas.HasValue || quotas.Value.HasNone();
        }

        bool IsFirstSync()
        {
            return !previousTier.HasValue;
        }

        bool AreTiersTheSame()
        {
            return previousTier.Value == currentTier
                   && currentTier != BillingSubscriptionTier.Unsubscribed;
        }
    }
}

internal static class SubscriptionQuotasExtensions
{
    public static SubscriptionQuotaUsage? FindUsageForSubscriptionAndProvider(this List<SubscriptionQuotaUsage> usages,
        SubscriptionRoot subscription, string providerName, BillingSubscriptionTier tier, string quotaId)
    {
        return usages.FirstOrDefault(use =>
            use.ProviderName == providerName
            && use.SubscriptionTier == tier
            && use.SubscriptionId == subscription.Id
            && use.QuotaId == quotaId);
    }
}