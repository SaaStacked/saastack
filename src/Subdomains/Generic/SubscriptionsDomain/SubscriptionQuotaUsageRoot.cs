using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Subscriptions.Quotas;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using Domain.Shared.Subscriptions;
using JetBrains.Annotations;
using QueryAny;

namespace SubscriptionsDomain;

/// <summary>
///     This root is snapshotted since quotas change once everytime the total changes, and for quotas with high limits or
///     that are unlimited, we could be generating many hundreds or thousands of change events, over teh lifecycle of this
///     root.
/// </summary>
[EntityName("SubscriptionQuotaUsage")]
public sealed class SubscriptionQuotaUsageRoot : AggregateRootBase
{
    public static Result<SubscriptionQuotaUsageRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier subscriptionId, Identifier owningEntityId, string providerName)
    {
        if (providerName.IsInvalidParameter(Validations.Subscription.ProviderName, nameof(providerName),
                Resources.SubscriptionQuotaUsageRoot_InvalidProviderName,
                out var error1))
        {
            return error1;
        }

        var root = new SubscriptionQuotaUsageRoot(recorder, idFactory);
        root.RaiseCreateEvent(
            SubscriptionsDomain.Events.Quotas.Created(root.Id, subscriptionId, owningEntityId, providerName));
        return root;
    }

    private SubscriptionQuotaUsageRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private SubscriptionQuotaUsageRoot(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
        LastResetAt = rehydratingProperties.GetValueOrDefault<DateTime>(nameof(LastResetAt));
        Limit = rehydratingProperties.GetValueOrDefault<long>(nameof(Limit));
        OwningEntityId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(OwningEntityId));
        Period = rehydratingProperties.GetValueOrDefault<BillingSubscriptionQuotaPeriod>(nameof(Period));
        ProviderName = rehydratingProperties.GetValueOrDefault<string>(nameof(ProviderName));
        QuotaId = rehydratingProperties.GetValueOrDefault<string>(nameof(QuotaId));
        SubscriptionId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(SubscriptionId));
        SubscriptionTier = rehydratingProperties.GetValueOrDefault<BillingSubscriptionTier>(nameof(SubscriptionTier));
        Total = rehydratingProperties.GetValueOrDefault<long>(nameof(Total));
    }

    public Optional<DateTime> LastResetAt { get; private set; }

    public long Limit { get; private set; } = -1;

    public Identifier OwningEntityId { get; private set; } = Identifier.Empty();

    public BillingSubscriptionQuotaPeriod Period { get; private set; } = BillingSubscriptionQuotaPeriod.Eternity;

    public Optional<string> ProviderName { get; private set; }

    public Optional<string> QuotaId { get; private set; }

    public Optional<Identifier> SubscriptionId { get; private set; }

    public BillingSubscriptionTier SubscriptionTier { get; private set; } = BillingSubscriptionTier.Unsubscribed;

    public long Total { get; private set; }

    public override HydrationProperties Dehydrate()
    {
        var properties = base.Dehydrate();
        properties.Add(nameof(LastResetAt), LastResetAt);
        properties.Add(nameof(Limit), Limit);
        properties.Add(nameof(OwningEntityId), OwningEntityId);
        properties.Add(nameof(Period), Period);
        properties.Add(nameof(ProviderName), ProviderName);
        properties.Add(nameof(QuotaId), QuotaId);
        properties.Add(nameof(SubscriptionId), SubscriptionId);
        properties.Add(nameof(SubscriptionTier), SubscriptionTier);
        properties.Add(nameof(Total), Total);
        return properties;
    }

    [UsedImplicitly]
    public static AggregateRootFactory<SubscriptionQuotaUsageRoot> Rehydrate()
    {
        return (identifier, container, properties) => new SubscriptionQuotaUsageRoot(identifier, container, properties);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        //TODO: add your other invariant rules here

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                SubscriptionId = created.SubscriptionId.ToId();
                OwningEntityId = created.OwningEntityId.ToId();
                ProviderName = created.ProviderName;
                return Result.Ok;
            }

            case Configured configured:
            {
                SubscriptionId = configured.SubscriptionId.ToId();
                LastResetAt = configured.LastResetAt;
                Limit = configured.Limit;
                SubscriptionTier = configured.SubscriptionTier;
                Period = configured.Period;
                QuotaId = configured.QuotaId;
                Total = configured.Total;

                Recorder.TraceDebug(null, "Subscription Quota Usage {Id} was configured", Id);
                return Result.Ok;
            }

            case TotalChanged changed:
            {
                Total = changed.Total;

                Recorder.TraceDebug(null, "Subscription Quota Usage {Id} total changed", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> Configure(BillingSubscriptionTier tier, string quotaId,
        ProviderPlanQuota quota)
    {
        var hasChanged = tier != SubscriptionTier
                         || quotaId != QuotaId
                         || quota.Limit != Limit
                         || quota.Period != Period;
        if (!hasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(SubscriptionsDomain.Events.Quotas.Configured(Id,
            SubscriptionId, OwningEntityId, ProviderName, tier, quotaId, quota.Period, quota.Limit));
    }

    public Result<Error> SetTotal(long total)
    {
        if (total.IsInvalidParameter(IsPositive, nameof(total), Resources.SubscriptionQuotaUsageRoot_InvalidTotal,
                out var error))
        {
            return error;
        }

        if (Limit != -1
            && total > Limit)
        {
            return Error.FeatureViolation(
                Resources.SubscriptionsQuotaUsageRoot_LimitExceeded.Format(QuotaId, Limit,
                    SubscriptionTier));
        }

        var hasChanged = total != Total;
        if (!hasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(SubscriptionsDomain.Events.Quotas.TotalChanged(Id,
            SubscriptionId, OwningEntityId, ProviderName, SubscriptionTier, QuotaId, Period, Limit, LastResetAt,
            total));

        bool IsPositive(long tot)
        {
            return tot >= 0L;
        }
    }
}