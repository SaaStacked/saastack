using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class SubscriptionConverted : DomainEvent
{
    public SubscriptionConverted(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SubscriptionConverted()
    {
    }

    public required string OwningEntityId { get; set; }

    public required string PlanId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

    public required string SubscriptionReference { get; set; }
}