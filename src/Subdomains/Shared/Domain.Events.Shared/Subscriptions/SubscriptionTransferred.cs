using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Subscriptions;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class SubscriptionTransferred : DomainEvent
{
    public SubscriptionTransferred(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SubscriptionTransferred()
    {
    }

    public required string BuyerReference { get; set; }

    public required string FromBuyerId { get; set; }

    public required string OwningEntityId { get; set; }

    public required string PlanId { get; set; }

    public required BillingSubscriptionTier PlanTier { get; set; }

    public ManagedQuotaDefinitions? PlanTierQuotas { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

    public string? SubscriptionReference { get; set; }

    public required string ToBuyerId { get; set; }

    public required string TransferredById { get; set; }
}