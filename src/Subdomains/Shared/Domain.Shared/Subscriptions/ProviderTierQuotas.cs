using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderTierQuotas : ValueObjectBase<ProviderTierQuotas>
{
    public static Result<ProviderTierQuotas, Error> Create(BillingSubscriptionTier tier,
        Optional<ProviderPlanQuotas> quotas)
    {
        return new ProviderTierQuotas(tier, quotas);
    }

    private ProviderTierQuotas(BillingSubscriptionTier tier, Optional<ProviderPlanQuotas> quotas)
    {
        Tier = tier;
        Quotas = quotas;
    }

    public Optional<ProviderPlanQuotas> Quotas { get; }

    public BillingSubscriptionTier Tier { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderTierQuotas> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderTierQuotas(
                parts[0].Value.ToEnum<BillingSubscriptionTier>(),
                parts[1].HasValue
                    ? ProviderPlanQuotas.Rehydrate()(parts[1], container)
                    : Optional<ProviderPlanQuotas>.None);
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Tier, Quotas];
    }
}