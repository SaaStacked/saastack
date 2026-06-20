using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Subscriptions;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class SubscriptionCanceled : DomainEvent
{
    public SubscriptionCanceled(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SubscriptionCanceled()
    {
    }

    public required string CanceledById { get; set; }

    public required string FromPlanId { get; set; }

    public required BillingSubscriptionTier FromPlanTier { get; set; }

    public required string OwningEntityId { get; set; }

    public ManagedQuotaDefinitions? PlanTierQuotas { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

    public required BillingSubscriptionTier ToPlanTier { get; set; }
}