using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Subscriptions;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions.Quotas;

public sealed class TotalChanged : DomainEvent
{
    public TotalChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TotalChanged()
    {
    }

    public required BillingSubscriptionTier SubscriptionTier { get; set; }

    public required DateTime LastResetAt { get; set; }

    public required long Limit { get; set; }

    public required string OwningEntityId { get; set; }

    public required BillingSubscriptionQuotaPeriod Period { get; set; }

    public required string ProviderName { get; set; }

    public required string QuotaId { get; set; }

    public required string SubscriptionId { get; set; }

    public required long Total { get; set; }
}