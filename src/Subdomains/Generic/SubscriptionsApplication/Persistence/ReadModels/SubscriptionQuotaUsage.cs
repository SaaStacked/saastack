using Application.Persistence.Common;
using Common;
using Domain.Shared.Subscriptions;
using QueryAny;

namespace SubscriptionsApplication.Persistence.ReadModels;

[EntityName("SubscriptionQuotaUsage")]
public class SubscriptionQuotaUsage : ReadModelEntity
{
    public Optional<DateTime> LastResetAt { get; set; }

    public Optional<long> Limit { get; set; }

    public Optional<string> OwningEntityId { get; set; }

    public BillingSubscriptionQuotaPeriod Period { get; set; }

    public Optional<string> ProviderName { get; set; }

    public Optional<string> QuotaId { get; set; }

    public Optional<string> SubscriptionId { get; set; }

    public BillingSubscriptionTier SubscriptionTier { get; set; }

    public Optional<long> Total { get; set; }
}