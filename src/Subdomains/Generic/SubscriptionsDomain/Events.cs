using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Subscriptions;
using Domain.Shared.Subscriptions;
using Created = Domain.Events.Shared.Subscriptions.Created;
using Deleted = Domain.Events.Shared.Subscriptions.Deleted;

namespace SubscriptionsDomain;

public static class Events
{
    public static BuyerDetailsChanged BuyerDetailsChanged(Identifier id, Identifier owningEntityId,
        BillingProvider provider)
    {
        return new BuyerDetailsChanged(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State
        };
    }

    public static BuyerRestored BuyerRestored(Identifier id, Identifier owningEntityId,
        BillingProvider provider, string buyerReference, Optional<string> subscriptionReference)
    {
        return new BuyerRestored(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference.ToNullable()
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

    public static PaymentMethodChanged PaymentMethodChanged(Identifier id, Identifier owningEntityId,
        BillingProvider provider)
    {
        return new PaymentMethodChanged(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State
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
        BillingProvider provider)
    {
        return new SubscriptionCanceled(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State
        };
    }

    public static SubscriptionConverted SubscriptionConverted(Identifier id, Identifier owningEntityId,
        BillingProvider provider, string planId, string subscriptionReference)
    {
        return new SubscriptionConverted(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            PlanId = planId,
            SubscriptionReference = subscriptionReference
        };
    }

    public static SubscriptionPlanChanged SubscriptionPlanChanged(Identifier id, Identifier owningEntityId,
        string planId, BillingProvider provider, string buyerReference, Optional<string> subscriptionReference)
    {
        return new SubscriptionPlanChanged(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference.ToNullable(),
            PlanId = planId
        };
    }

    public static SubscriptionTransferred SubscriptionTransferred(Identifier id, Identifier owningEntityId,
        Identifier transfererId, Identifier transfereeId, string planId, BillingProvider provider,
        string buyerReference, Optional<string> subscriptionReference)
    {
        return new SubscriptionTransferred(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference.ToNullable(),
            PlanId = planId,
            FromBuyerId = transfererId,
            ToBuyerId = transfereeId
        };
    }

    public static SubscriptionUnsubscribed SubscriptionUnsubscribed(Identifier id, Identifier owningEntityId,
        BillingProvider provider)
    {
        return new SubscriptionUnsubscribed(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State
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
        Identifier owningEntityId,
        BillingProvider provider, TrialScheduledEvent @event)
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

    public static ManagedTrialEventScheduleEnded ManagedTrialEventScheduleEnded(Identifier id,
        Identifier owningEntityId,
        BillingProvider provider, TrialScheduledEndingReason reason)
    {
        return new ManagedTrialEventScheduleEnded(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            Reason = reason
        };
    }

    public static ManagedTrialStarted ManagedTrialStarted(Identifier id, Identifier owningEntityId,
        BillingProvider provider,
        TrialTimeline trial)
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
}