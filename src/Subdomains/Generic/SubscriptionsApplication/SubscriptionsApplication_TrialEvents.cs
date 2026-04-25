using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.Extensions;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared;
using Domain.Shared.Subscriptions;
using SubscriptionsDomain;

namespace SubscriptionsApplication;

public partial class SubscriptionsApplication
{
    private readonly ISubscriptionTrialEventMessageQueueRepository _trialEventMessageRepository;

    public async Task<Result<bool, Error>> DeliverSubscriptionTrialEventAsync(ICallerContext caller,
        string messageAsJson, CancellationToken cancellationToken)
    {
        var rehydrated = messageAsJson.RehydrateQueuedMessage<SubscriptionTrialEventMessage>();
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var delivered =
            await DeliverSubscriptionTrialEventInternalAsync(caller, rehydrated.Value, cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered trial event message: {Message}", messageAsJson);
        return true;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllSubscriptionTrialEventsAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        await _trialEventMessageRepository.DrainAllQueuedMessagesAsync(_recorder,
            message => DeliverSubscriptionTrialEventInternalAsync(caller, message, cancellationToken),
            cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all trial event messages");

        return Result.Ok;
    }
#endif

    private async Task<Result<bool, Error>> DeliverSubscriptionTrialEventInternalAsync(ICallerContext caller,
        SubscriptionTrialEventMessage message, CancellationToken cancellationToken)
    {
        if (message.OwningEntityId.IsInvalidParameter(x => x.HasValue(),
                nameof(SubscriptionTrialEventMessage.OwningEntityId), out _))
        {
            return Error.RuleViolation(Resources.SubscriptionsApplication_TrialEvent_MissingOwningEntityId);
        }

        if (message.ProviderName.IsInvalidParameter(x => x.HasValue(),
                nameof(SubscriptionTrialEventMessage.ProviderName), out _))
        {
            return Error.RuleViolation(Resources.SubscriptionsApplication_TrialEvent_MissingProviderName);
        }

        if (message.Event.NotExists() && message.Signal.NotExists())
        {
            return Error.RuleViolation(Resources.SubscriptionsApplication_TrialEvent_MissingEventAndSignal);
        }

        var owningEntityId = message.OwningEntityId!.ToId();
        var retrieved = await _repository.FindByOwningEntityIdAsync(owningEntityId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            _recorder.TraceWarning(caller.ToCall(),
                "Trial event (from {Region}) destined for the subscription for owning entity {Id} was not found",
                owningEntityId, message.OriginHostRegion ?? DatacenterLocations.Unknown.Code);
            return true; // ignore the event
        }

        var subscription = retrieved.Value.Value;
        if (message.Signal.Exists())
        {
            var signalId = message.Signal.SignalId;
            var handled =
                await subscription.HandleManagedTrialExpiredSignalAsync(_billingProvider.StateInterpreter,
                    OnDispatchTrialSignal, OnExpiredTrial, OnDispatchTrialEvent);
            if (handled.IsFailure)
            {
                return handled.Error;
            }

            var saved = await _repository.SaveAsync(subscription, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            subscription = saved.Value;
            _recorder.TraceInformation(caller.ToCall(),
                "Trial signal {EventId} for subscription {Id} (from {Region}) was delivered",
                signalId, subscription.Id, message.OriginHostRegion ?? DatacenterLocations.Unknown.Code);

            return true;
        }
        else
        {
            var scheduledEvent = TrialScheduledEvent.Create(message.Event!.DelayInDays, message.Event.EventId,
                message.Event.Track.ToEnumOrDefault(TrialScheduledEventTrack.Active),
                message.Event.Action.ToEnumOrDefault(TrialScheduledEventAction.Notification),
                StringNameValues.Create(message.Event.Metadata).Value);
            if (scheduledEvent.IsFailure)
            {
                return scheduledEvent.Error;
            }

            var trialEvent = scheduledEvent.Value;
            var delivered = await subscription.DeliverManagedTrialScheduledEventAsync(_billingProvider.StateInterpreter,
                trialEvent, OnDeliverTrialEvent, OnDispatchTrialEvent);
            if (delivered.IsFailure)
            {
                return delivered.Error;
            }

            var saved = await _repository.SaveAsync(subscription, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            subscription = saved.Value;
            _recorder.TraceInformation(caller.ToCall(),
                "Trial event {EventId} for subscription {Id} (from {Region}) was delivered to provider",
                trialEvent.Id, subscription.Id, message.OriginHostRegion ?? DatacenterLocations.Unknown.Code);

            return true;
        }

        async Task<Result<Error>> OnExpiredTrial(SubscriptionRoot root)
        {
            return await OnExpireManagedTrialAsync(caller, root, cancellationToken);
        }

        async Task<Result<Error>> OnDispatchTrialSignal(SubscriptionRoot root)
        {
            return await OnDispatchManagedTrialSignalAsync(caller, root, cancellationToken);
        }

        async Task<Result<Error>> OnDeliverTrialEvent(SubscriptionRoot root, TrialScheduledEvent current)
        {
            var owningEntity =
                await _subscriptionOwningEntityService.GetEntityAsync(caller, root.OwningEntityId, cancellationToken);
            if (owningEntity.IsFailure)
            {
                return owningEntity.Error;
            }

            var buyer = await CreateBuyerAsync(caller, root.BuyerId, owningEntity.Value, cancellationToken);
            if (buyer.IsFailure)
            {
                return buyer.Error;
            }

            return await _billingProvider.GatewayService.HandleTrialScheduledEventAsync(caller,
                buyer.Value.Value,
                current, root.Provider, cancellationToken);
        }

        async Task<Result<Error>> OnDispatchTrialEvent(SubscriptionRoot root, TrialScheduledEvent next,
            DateTime relativeTo)
        {
            return await OnDispatchManagedTrialEventAsync(caller, root, next, relativeTo, cancellationToken);
        }
    }

    private async Task<Result<Error>> OnDispatchManagedTrialEventAsync(ICallerContext caller,
        SubscriptionRoot subscription, TrialScheduledEvent @event, DateTime relativeTo,
        CancellationToken cancellationToken)
    {
        var queueDelay = relativeTo
            .ToNearestHour()
            .AddDays(@event.DelayInDays)
            .Subtract(DateTime.UtcNow.ToNearestHour());

        if (queueDelay < TimeSpan.Zero)
        {
            _recorder.TraceWarning(caller.ToCall(),
                "Subscription {Id} ignored trial event {EventId} for {BuyerId}, as it was past its scheduled delivery date",
                subscription.Id, @event.Id, subscription.BuyerId);
            return Result.Ok;
        }

        var trialMessage = new SubscriptionTrialEventMessage
        {
            OwningEntityId = subscription.OwningEntityId,
            ProviderName = subscription.Provider.Value.Name,

            Event = new QueuedTrialEvent
            {
                EventId = @event.Id,
                DelayInDays = @event.DelayInDays,
                Track = @event.Track.ToString(),
                Action = @event.Action.ToString(),
                Metadata = @event.Metadata.Items.ToDictionary()
            }
        };

        var queued =
            await _trialEventMessageRepository.PushAsync(caller.ToCall(), trialMessage, queueDelay, cancellationToken);
        if (queued.IsFailure)
        {
            return queued.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Subscription {Id} dispatched trial event {EventId} for {BuyerId}",
            subscription.Id, @event.Id, subscription.BuyerId);

        return Result.Ok;
    }

    /// <summary>
    ///     Signals are dispatched back to back until we have expired the trial.
    ///     Messages appear on the queue after a delay, but some queue technologies have maximums for the length
    ///     of the maximum delay
    /// </summary>
    private async Task<Result<Error>> OnDispatchManagedTrialSignalAsync(ICallerContext caller,
        SubscriptionRoot subscription, CancellationToken cancellationToken)
    {
        if (!subscription.ManagedTrial.HasValue)
        {
            return Result.Ok;
        }

        var idealDelayUntilExpirable = subscription.ManagedTrial.Value.ExpiryDueAt
            .Subtract(DateTime.UtcNow.ToNearestMinute())
            .Add(TimeSpan.FromMinutes(5)); // we want to be sure we account for clock skews and any processing errors

        var requiredDelay = idealDelayUntilExpirable;
        if (idealDelayUntilExpirable >= _trialEventMessageRepository.MaxMessageDelay)
        {
            // Will need to queue more than one signal after this one
            requiredDelay = _trialEventMessageRepository.MaxMessageDelay;
        }

        var signalId = GenerateSignalId();
        var trialMessage = new SubscriptionTrialEventMessage
        {
            OwningEntityId = subscription.OwningEntityId,
            ProviderName = subscription.Provider.Value.Name,
            Signal = new QueuedTrialSignal
            {
                SignalId = signalId
            }
        };

        var queued =
            await _trialEventMessageRepository.PushAsync(caller.ToCall(), trialMessage, requiredDelay,
                cancellationToken);
        if (queued.IsFailure)
        {
            return queued.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Subscription {Id} dispatched trial signal {EventId} for {BuyerId}, of {Delay:}",
            subscription.Id, signalId, subscription.BuyerId, requiredDelay.ToString("g"));

        return Result.Ok;

        static string GenerateSignalId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 16);
        }
    }

    private async Task<Result<Error>> OnExpireManagedTrialAsync(ICallerContext caller, SubscriptionRoot subscription,
        CancellationToken cancellationToken)
    {
        var provider = subscription.Provider.Value;
        var subscriptionCanceled = await _billingProvider.GatewayService.CancelSubscriptionAsync(caller,
            CancelSubscriptionOptions.Immediately, provider, cancellationToken);
        if (subscriptionCanceled.IsFailure)
        {
            return subscriptionCanceled.Error;
        }

        provider = provider.ChangeState(subscriptionCanceled.Value);
        var cancellerId = CallerConstants.MaintenanceAccountUserId.ToId();
        var canceled =
            subscription.CancelSubscriptionByProvider(_billingProvider.StateInterpreter, cancellerId, provider);
        if (canceled.IsFailure)
        {
            return canceled.Error;
        }

        var saved = await _repository.SaveAsync(subscription, true, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        subscription = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Trial expired for subscription {Id}, buyer {BuyerId}, and subscription has been canceled",
            subscription.Id, subscription.BuyerId);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.SubscriptionCanceled,
            subscription.ToSubscriptionChangedUsageEvent());
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.SubscriptionManagedTrialExpired,
            subscription.ToSubscriptionChangedUsageEvent());

        return Result.Ok;
    }
}