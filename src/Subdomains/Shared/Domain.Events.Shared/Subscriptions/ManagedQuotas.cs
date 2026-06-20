using Common;
using Common.Extensions;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;

namespace Domain.Events.Shared.Subscriptions;

public class ManagedQuotaDefinitions
{
    public required IReadOnlyDictionary<string, ManagedQuotaDefinition> Quotas { get; set; }
}

public class ManagedQuotaDefinition
{
    public required string Description { get; set; }

    public required long Limit { get; set; }

    public BillingSubscriptionQuotaPeriod Period { get; set; }
}

public static class QuotaEventExtensions
{
    public static ManagedQuotaDefinitions ToManagedQuotaDefinitions(this ProviderTierQuotas quotas)
    {
        return new ManagedQuotaDefinitions
        {
            Quotas = quotas.Quotas.Value.Items.ToDictionary(pair2 => pair2.Key, pair2 => new ManagedQuotaDefinition
            {
                Description = pair2.Value.Description,
                Limit = pair2.Value.Limit,
                Period = pair2.Value.Period
            })
        };
    }

    public static ManagedQuotaDefinitions ToManagedQuotaDefinitions(this ProviderPlanQuotas quotas)
    {
        return new ManagedQuotaDefinitions
        {
            Quotas = quotas.Items.ToDictionary(pair2 => pair2.Key, pair2 => new ManagedQuotaDefinition
            {
                Description = pair2.Value.Description,
                Limit = pair2.Value.Limit,
                Period = pair2.Value.Period
            })
        };
    }

    public static Result<ProviderTierQuotas, Error> ToTierQuotas(
        this Dictionary<BillingSubscriptionTier, ManagedQuotaDefinitions> managedQuotas, BillingSubscriptionTier tier)
    {
        var tierQuotas = Optional<ProviderPlanQuotas>.None;
        if (managedQuotas.TryGetValue(tier, out var tierQuota))
        {
            var quotas = new Dictionary<string, ProviderPlanQuota>();
            foreach (var (quotaId, pair) in tierQuota.Quotas)
            {
                var planQuota = ProviderPlanQuota.Create(pair.Description, pair.Limit, pair.Period);
                if (planQuota.IsFailure)
                {
                    return planQuota.Error;
                }

                quotas.Add(quotaId, planQuota.Value);
            }

            var planQuotas = ProviderPlanQuotas.Create(quotas);
            if (planQuotas.IsFailure)
            {
                return planQuotas.Error;
            }

            tierQuotas = planQuotas.Value;
        }

        return ProviderTierQuotas.Create(tier, tierQuotas);
    }

    public static Result<ProviderTierQuotas, Error> ToTierQuotas(this ManagedQuotaDefinitions? managedTierQuotas,
        BillingSubscriptionTier tier)
    {
        if (managedTierQuotas.NotExists())
        {
            return ProviderTierQuotas.Create(tier, new Optional<ProviderPlanQuotas>());
        }

        var quotas = new Dictionary<string, ProviderPlanQuota>();
        foreach (var (quotaId, pair) in managedTierQuotas.Quotas)
        {
            var planQuota = ProviderPlanQuota.Create(pair.Description, pair.Limit, pair.Period);
            if (planQuota.IsFailure)
            {
                return planQuota.Error;
            }

            quotas.Add(quotaId, planQuota.Value);
        }

        var providerPlanQuotas = Optional<ProviderPlanQuotas>.None;
        if (quotas.HasAny())
        {
            var planQuotas = ProviderPlanQuotas.Create(quotas);
            if (planQuotas.IsFailure)
            {
                return planQuotas.Error;
            }

            providerPlanQuotas = planQuotas.Value;
        }

        return ProviderTierQuotas.Create(tier, providerPlanQuotas);
    }

    public static Optional<ProviderTierQuotas> ToTierQuotas(this BillingProviderCapabilities capabilities,
        BillingSubscriptionTier tier)
    {
        if (capabilities.QuotaManagement is not ManagementOptions.RequiresManaged)
        {
            return Optional<ProviderTierQuotas>.None;
        }

        var quotas = capabilities.ManagedQuotas;
        if (quotas.NotExists() || quotas.HasNone())
        {
            return Optional<ProviderTierQuotas>.None;
        }

        var tierQuotas = quotas.ForTier(tier);
        if (!tierQuotas.HasValue)
        {
            return Optional<ProviderTierQuotas>.None;
        }

        return ProviderTierQuotas.Create(tier, tierQuotas).Value;
    }
}