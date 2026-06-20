using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Subscriptions;
using Domain.Events.Shared.Subscriptions.Quotas;
using Domain.Shared.Subscriptions;
using Created = Domain.Events.Shared.Subscriptions.Created;
using Deleted = Domain.Events.Shared.Subscriptions.Deleted;

namespace SubscriptionsDomain;

public static class Events
{
    public static BuyerDetailsChanged BuyerDetailsChanged(Identifier id, Identifier owningEntityId,
        BillingProvider provider, Identifier modifiedById)
    {
        return new BuyerDetailsChanged(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            ModifiedById = modifiedById
        };
    }

    public static BuyerRestored BuyerRestored(Identifier id, Identifier owningEntityId,
        BillingProvider provider, string buyerReference, Optional<string> subscriptionReference,
        Identifier modifiedById)
    {
        return new BuyerRestored(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference.ToNullable(),
            ModifiedById = modifiedById
        };
    }

    public static Created Created(Identifier id, Identifier owningEntityId, Identifier buyerId, string providerName)
    {
        return new Created(id)
        {
            OwningEntityId = owningEntityId,
            BuyerId = buyerId,
            ProviderName = providerName
        };
    }

    public static Deleted Deleted(Identifier id, Identifier deletedById)
    {
        return new Deleted(id, deletedById);
    }

    public static ManagedQuotasStarted ManagedQuotasStarted(Identifier id, Identifier owningEntityId,
        BillingProvider provider, ProviderQuotas quotas, BillingSubscriptionTier tier)
    {
        return new ManagedQuotasStarted(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            Quotas = quotas.Items.ToDictionary(pair => pair.Key, pair => pair.Value.ToManagedQuotaDefinitions()),
            StartingTier = tier
        };
    }

    public static ManagedTrialEventScheduleEnded ManagedTrialEventScheduleEnded(Identifier id,
        Identifier owningEntityId, BillingProvider provider, TrialScheduledEndingReason reason)
    {
        return new ManagedTrialEventScheduleEnded(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            Reason = reason
        };
    }

    public static ManagedTrialExpired ManagedTrialExpired(Identifier id, Identifier owningEntityId,
        BillingProvider provider, TrialTimeline trial)
    {
        return new ManagedTrialExpired(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            TrialStartedAt = trial.StartedAt,
            TrialDurationDays = trial.DurationDays,
            TrialExpiresAt = trial.ExpiryDueAt
        };
    }

    public static ManagedTrialScheduledEventAdded ManagedTrialScheduledEventAdded(Identifier id,
        Identifier owningEntityId, BillingProvider provider, TrialScheduledEvent @event)
    {
        return new ManagedTrialScheduledEventAdded(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            EventId = @event.Id,
            EventAction = @event.Action,
            EventAppliesWhen = @event.Track,
            EventMetadata = @event.Metadata.Items.ToDictionary()
        };
    }

    public static ManagedTrialStarted ManagedTrialStarted(Identifier id, Identifier owningEntityId,
        BillingProvider provider, TrialTimeline trial)
    {
        return new ManagedTrialStarted(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            TrialStartedAt = trial.StartedAt,
            TrialDurationDays = trial.DurationDays
        };
    }

    public static PaymentMethodChanged PaymentMethodChanged(Identifier id, Identifier owningEntityId,
        BillingProvider provider, Identifier modifiedById)
    {
        return new PaymentMethodChanged(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            ModifiedById = modifiedById
        };
    }

    public static ProviderChanged ProviderChanged(Identifier id, Identifier owningEntityId, string? fromProviderName,
        BillingProvider provider, string buyerReference, Optional<string> subscriptionReference)
    {
        return new ProviderChanged(id)
        {
            OwningEntityId = owningEntityId,
            FromProviderName = fromProviderName,
            ToProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference.ToNullable()
        };
    }

    public static SubscriptionCanceled SubscriptionCanceled(Identifier id, Identifier owningEntityId,
        BillingProvider provider, string fromPlanId, BillingSubscriptionTier fromTier, Identifier canceledById)
    {
        return new SubscriptionCanceled(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            FromPlanId = fromPlanId,
            FromPlanTier = fromTier,
            ToPlanTier = BillingSubscriptionTier.Unsubscribed,
            PlanTierQuotas = null,
            CanceledById = canceledById
        };
    }

    public static SubscriptionConverted SubscriptionConverted(Identifier id, Identifier owningEntityId,
        BillingProvider provider, string planId, BillingSubscriptionTier tier, string subscriptionReference,
        Identifier convertedById, Optional<ProviderTierQuotas> quotas)
    {
        return new SubscriptionConverted(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            PlanId = planId,
            PlanTier = tier,
            PlanTierQuotas = quotas.ToNullable(x => x.ToManagedQuotaDefinitions()),
            SubscriptionReference = subscriptionReference,
            ConvertedById = convertedById
        };
    }

    public static SubscriptionPlanChanged SubscriptionPlanChanged(Identifier id, Identifier owningEntityId,
        string planId, BillingSubscriptionTier tier, BillingProvider provider, string buyerReference,
        Optional<string> subscriptionReference, Identifier modifiedById, Optional<ProviderTierQuotas> quotas)
    {
        return new SubscriptionPlanChanged(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference.ToNullable(),
            PlanId = planId,
            PlanTier = tier,
            PlanTierQuotas = quotas.ToNullable(x => x.ToManagedQuotaDefinitions()),
            ModifiedById = modifiedById
        };
    }

    public static SubscriptionTransferred SubscriptionTransferred(Identifier id, Identifier owningEntityId,
        Identifier transfererId, Identifier transfereeId, string planId, BillingSubscriptionTier tier,
        BillingProvider provider, string buyerReference, Optional<string> subscriptionReference,
        Identifier transferredById, Optional<ProviderTierQuotas> quotas)
    {
        return new SubscriptionTransferred(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference.ToNullable(),
            PlanId = planId,
            PlanTier = tier,
            PlanTierQuotas = quotas.ToNullable(x => x.ToManagedQuotaDefinitions()),
            FromBuyerId = transfererId,
            ToBuyerId = transfereeId,
            TransferredById = transferredById
        };
    }

    public static SubscriptionUnsubscribed SubscriptionUnsubscribed(Identifier id, Identifier owningEntityId,
        BillingProvider provider, Identifier unsubscribedById)
    {
        return new SubscriptionUnsubscribed(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            UnsubscribedById = unsubscribedById
        };
    }

    public static class Quotas
    {
        public static Configured Configured(Identifier id,
            Identifier subscriptionId, Identifier owningEntityId, string providerName,
            BillingSubscriptionTier tier, string quotaId,
            BillingSubscriptionQuotaPeriod period, long limit)
        {
            return new Configured(id)
            {
                OwningEntityId = owningEntityId,
                ProviderName = providerName,
                SubscriptionId = subscriptionId,
                SubscriptionTier = tier,
                Period = period,
                Limit = limit,
                QuotaId = quotaId,
                Total = 0,
                LastResetAt = DateTime.UtcNow
            };
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static Domain.Events.Shared.Subscriptions.Quotas.Created Created(Identifier id,
            Identifier subscriptionId,
            Identifier owningEntityId, string providerName)
        {
            return new Domain.Events.Shared.Subscriptions.Quotas.Created(id)
            {
                SubscriptionId = subscriptionId,
                OwningEntityId = owningEntityId,
                ProviderName = providerName
            };
        }

        public static TotalChanged TotalChanged(Identifier id,
            Identifier subscriptionId, Identifier owningEntityId, string providerName,
            BillingSubscriptionTier tier, string quotaId,
            BillingSubscriptionQuotaPeriod period, long limit, DateTime lastResetAt, long total)
        {
            return new TotalChanged(id)
            {
                OwningEntityId = owningEntityId,
                ProviderName = providerName,
                SubscriptionId = subscriptionId,
                SubscriptionTier = tier,
                Period = period,
                Limit = limit,
                QuotaId = quotaId,
                Total = total,
                LastResetAt = lastResetAt
            };
        }
    }
}