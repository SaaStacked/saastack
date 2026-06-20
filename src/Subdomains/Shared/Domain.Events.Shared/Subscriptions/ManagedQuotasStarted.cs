using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Subscriptions;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class ManagedQuotasStarted : DomainEvent
{
    public ManagedQuotasStarted(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ManagedQuotasStarted()
    {
    }

    public required string OwningEntityId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

#pragma warning disable SAASDDD049
    // we cannot serialize value objects, so we need to convert to DTOs
    public required Dictionary<BillingSubscriptionTier, ManagedQuotaDefinitions> Quotas { get; set; }
#pragma warning restore SAASDDD049
    public required BillingSubscriptionTier StartingTier { get; set; }
}