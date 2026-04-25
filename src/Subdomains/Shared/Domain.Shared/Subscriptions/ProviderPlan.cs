using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderPlan : ValueObjectBase<ProviderPlan>
{
    public static readonly ProviderPlan Empty =
        new(Optional<string>.None, Optional<TrialTimeline>.None, BillingSubscriptionTier.Unsubscribed);

    public static Result<ProviderPlan, Error> Create(string planId, BillingSubscriptionTier tier)
    {
        if (planId.IsInvalidParameter(id => id.HasValue(), nameof(planId), Resources.ProviderPlan_InvalidPlanId,
                out var error))
        {
            return error;
        }

        return Create(planId, Optional<TrialTimeline>.None, tier);
    }

    public static Result<ProviderPlan, Error> Create(string planId, Optional<TrialTimeline> trial,
        BillingSubscriptionTier tier)
    {
        if (planId.IsInvalidParameter(id => id.HasValue(), nameof(planId), Resources.ProviderPlan_InvalidPlanId,
                out var error))
        {
            return error;
        }

        return new ProviderPlan(planId, trial, tier);
    }

    private ProviderPlan(Optional<string> planId, Optional<TrialTimeline> trial, BillingSubscriptionTier tier)
    {
        PlanId = planId;
        Trial = trial;
        Tier = tier;
    }

    public Optional<string> PlanId { get; }

    public BillingSubscriptionTier Tier { get; }

    public Optional<TrialTimeline> Trial { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPlan> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderPlan(
                parts[0],
                TrialTimeline.Rehydrate()(parts[1], container),
                parts[2].Value.ToEnumOrDefault(BillingSubscriptionTier.Unsubscribed));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [PlanId, Trial, Tier];
    }
}

public enum BillingSubscriptionTier
{
    // EXTEND: define other subscription tiers related to subscription plans
    Unsubscribed = 0,
    Standard = 1,
    Professional = 2,
    Enterprise = 3
}