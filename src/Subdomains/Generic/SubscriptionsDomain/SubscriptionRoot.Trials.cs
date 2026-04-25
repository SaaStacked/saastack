using Common;
using Common.Extensions;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;

namespace SubscriptionsDomain;

public delegate Task<Result<Error>> DispatchTrialEventAction(SubscriptionRoot subscription, TrialScheduledEvent @event,
    DateTime relativeTo);

public delegate Task<Result<Error>> DispatchTrialSignalAction(SubscriptionRoot subscription);

public delegate Task<Result<Error>> DeliverTrialEventAction(SubscriptionRoot subscription, TrialScheduledEvent @event);

public delegate Task<Result<Error>> ExpireTrialAction(SubscriptionRoot subscription);

/// <summary>
///     This aspect of a the <see cref="SubscriptionRoot" /> manages the trial for providers that cannot manage their own
///     trials, when the subscription is created.
/// </summary>
#pragma warning disable SAASDDD014
#pragma warning disable SAASDDD010
#pragma warning disable SAASDDD018
partial class SubscriptionRoot
#pragma warning restore SAASDDD018
#pragma warning restore SAASDDD010
#pragma warning restore SAASDDD014
{
    public bool IsTrialEventScheduleEnded { get; private set; }

    public Optional<string> LastScheduledTrialEventId { get; private set; }

    /// <summary>
    ///     The trial for this subscription is either self-managed by the provider
    ///     or it is fully managed by this root, depending on the
    ///     provider's <see cref="BillingProviderCapabilities.TrialManagement" />.
    ///     Note: This trial will never have the schedule attached to it. Use <see cref="TrialTimeline.AttachSchedule" /> to
    ///     attach before accessing the schedule.
    /// </summary>
    public Optional<TrialTimeline> ManagedTrial { get; private set; }

    /// <summary>
    ///     Delivers the specified <see cref="currentEvent" /> to the provider (as long as it matches the current state of the
    ///     trial), and then dispatches the next event in the schedule (for this current state of the trial).
    ///     If we receive a message for a different trial state, (or that no longer exists, etc.) we just ignore it,
    ///     and stop dispatching.
    ///     Dispatching events for different states occurs when those states are initiated.
    /// </summary>
    public async Task<Result<Error>> DeliverManagedTrialScheduledEventAsync(IBillingStateInterpreter interpreter,
        TrialScheduledEvent currentEvent, DeliverTrialEventAction onDeliverTrialEvent,
        DispatchTrialEventAction onDispatchNextEvent)
    {
        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        if (interpreter.Capabilities.TrialManagement is not TrialManagementOptions.RequiresManaged)
        {
            return Result.Ok;
        }

        if (IsTrialEventScheduleEnded)
        {
            return Result.Ok;
        }

        if (!ManagedTrial.HasValue)
        {
            Recorder.TraceWarning(null, "Trial no longer exists!");
            if (!IsTrialEventScheduleEnded)
            {
                return RaiseChangeEvent(SubscriptionsDomain.Events.ManagedTrialEventScheduleEnded(Id, OwningEntityId,
                    Provider,
                    TrialScheduledEndingReason.TrialMissing));
            }

            return Result.Ok;
        }

        var trial = ManagedTrial.Value;
        var schedule = interpreter.Capabilities.ManagedTrialSchedule;
        if (schedule.NotExists())
        {
            Recorder.TraceWarning(null, "Trial no longer contains a schedule!");
            if (!IsTrialEventScheduleEnded)
            {
                return RaiseChangeEvent(SubscriptionsDomain.Events.ManagedTrialEventScheduleEnded(Id, OwningEntityId,
                    Provider,
                    TrialScheduledEndingReason.TrialScheduleRemoved));
            }

            return Result.Ok;
        }

        if (schedule.Events.HasNone())
        {
            Recorder.TraceWarning(null, "Trial Schedule no longer contains any items to deliver!");
            if (!IsTrialEventScheduleEnded)
            {
                return RaiseChangeEvent(SubscriptionsDomain.Events.ManagedTrialEventScheduleEnded(Id, OwningEntityId,
                    Provider,
                    TrialScheduledEndingReason.TrialScheduleRemoved));
            }

            return Result.Ok;
        }

        if (!IsEventInSchedule(currentEvent))
        {
            Recorder.TraceWarning(null, "Trial Schedule no longer contains delivered item {Item}!", currentEvent.Id);
            return Result.Ok;
        }

        if (!trial.IsEventApplyToTrialState(currentEvent))
        {
            // We have an event for another state, perhaps arriving too late?
            // now irrelevant, ignore it
            return Result.Ok;
        }

        var delivered = await onDeliverTrialEvent(this, currentEvent);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        var actioned =
            RaiseChangeEvent(
                SubscriptionsDomain.Events.ManagedTrialScheduledEventAdded(Id, OwningEntityId, Provider, currentEvent));
        if (actioned.IsFailure)
        {
            return actioned.Error;
        }

        var trialStatus = trial.Status;
        var nextTrackEvent = FindNextEventForTrack(currentEvent.Id);
        if (nextTrackEvent.HasValue)
        {
            return await onDispatchNextEvent(this, nextTrackEvent, GetTrackCommencementDate(trialStatus));
        }

        //No more events in the current track
        if (!trial.IsConverted)
        {
            return Result.Ok;
        }

        //Trial schedule is all done
        if (!IsTrialEventScheduleEnded)
        {
            return RaiseChangeEvent(SubscriptionsDomain.Events.ManagedTrialEventScheduleEnded(Id, OwningEntityId,
                Provider,
                TrialScheduledEndingReason.NoMoreEvents));
        }

        return Result.Ok;

        bool IsEventInSchedule(TrialScheduledEvent @event)
        {
            return schedule.Events
                .FirstOrDefault(evt => evt.Id.EqualsIgnoreCase(@event.Id))
                .Exists();
        }

        Optional<TrialScheduledEvent> FindNextEventForTrack(string eventId)
        {
            return schedule.FilterEventTrack(trial.Status)
                .SkipWhile(evt => evt.Id.NotEqualsIgnoreCase(eventId))
                .Skip(1)
                .FirstOrDefault().ToOptional();
        }

        DateTime GetTrackCommencementDate(TrialStatus status)
        {
            return status switch
            {
                TrialStatus.Active => trial.StartedAt,
                TrialStatus.Converted => trial.ConvertedAt.Value,
                TrialStatus.Expired => trial.ExpiredAt.Value,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }

    public async Task<Result<Error>> DispatchManagedTrialFirstExpirySignalAsync(IBillingStateInterpreter interpreter,
        DispatchTrialSignalAction onDispatchSignal)
    {
        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        if (interpreter.Capabilities.TrialManagement is not TrialManagementOptions.RequiresManaged)
        {
            return Result.Ok;
        }

        if (!ManagedTrial.HasValue)
        {
            return Result.Ok;
        }

        return await onDispatchSignal(this);
    }

    public async Task<Result<Error>> DispatchManagedTrialFirstScheduledEventAsync(IBillingStateInterpreter interpreter,
        DispatchTrialEventAction onDispatchEvent)
    {
        if (interpreter.Capabilities.TrialManagement is not TrialManagementOptions.RequiresManaged)
        {
            return Result.Ok;
        }

        if (IsTrialEventScheduleEnded)
        {
            return Result.Ok;
        }

        if (!ManagedTrial.HasValue)
        {
            return Result.Ok;
        }

        var trial = ManagedTrial.Value;
        var schedule = interpreter.Capabilities.ManagedTrialSchedule;
        if (schedule.NotExists())
        {
            return Result.Ok;
        }

        return await DispatchFirstScheduledTrackEventInternalAsync(trial, schedule, onDispatchEvent);
    }

    public async Task<Result<Error>> ExpireManagedTrialAsync(IBillingStateInterpreter interpreter, TrialTimeline trial,
        ExpireTrialAction onTrialExpired, DispatchTrialEventAction onDispatchNextEvent)
    {
        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        if (interpreter.Capabilities.TrialManagement is not TrialManagementOptions.RequiresManaged)
        {
            return Result.Ok;
        }

        if (trial.IsExpired)
        {
            return Error.PreconditionViolation(Resources.SubscriptionRoot_ExpireTrial_AlreadyExpired);
        }

        if (!trial.IsExpirable)
        {
            return Error.PreconditionViolation(Resources.SubscriptionRoot_ExpireTrial_NotExpirable);
        }

        var expired =
            RaiseChangeEvent(SubscriptionsDomain.Events.ManagedTrialExpired(Id, OwningEntityId, Provider, trial));
        if (expired.IsFailure)
        {
            return expired.Error;
        }

        var handled = await onTrialExpired(this);
        if (handled.IsFailure)
        {
            return handled.Error;
        }

        var schedule = interpreter.Capabilities.ManagedTrialSchedule ?? TrialEventSchedule.Empty;
        return await DispatchFirstScheduledTrackEventInternalAsync(ManagedTrial.Value, schedule, onDispatchNextEvent);
    }

#if TESTINGONLY
    public async Task<Result<ProviderSubscription, Error>> ForceExpireManagedTrialAsync(
        IBillingStateInterpreter interpreter, ExpireTrialAction onTrialExpired,
        DispatchTrialEventAction onDispatchNextEvent)
    {
        var subscriptionDetails = interpreter.GetSubscriptionDetails(Provider);
        if (subscriptionDetails.IsFailure)
        {
            return subscriptionDetails.Error;
        }

        var providerSubscription = subscriptionDetails.Value;
        if (!ManagedTrial.HasValue)
        {
            return providerSubscription;
        }

        ManagedTrial = ManagedTrial.Value.TestingOnly_FastForwardToExpiry().Value;

        var trial = ManagedTrial.Value;
        var expired = await ExpireManagedTrialAsync(interpreter, trial, onTrialExpired, onDispatchNextEvent);
        if (expired.IsFailure)
        {
            return expired.Error;
        }

        return providerSubscription;
    }
#endif

    /// <summary>
    ///     Handle the latest signal to expire the trial.
    ///     If the trial is already converted, the trial is already over, and will never expire.
    ///     If the trial is already expired, the trial is already over
    ///     If the trial is past its expiry date, then we go ahead and expire it
    /// </summary>
    public async Task<Result<Error>> HandleManagedTrialExpiredSignalAsync(IBillingStateInterpreter interpreter,
        DispatchTrialSignalAction onDispatchNextSignal, ExpireTrialAction onTrialExpired,
        DispatchTrialEventAction onDispatchNextEvent)
    {
        if (interpreter.Capabilities.TrialManagement is not TrialManagementOptions.RequiresManaged)
        {
            return Result.Ok;
        }

        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        if (!ManagedTrial.HasValue)
        {
            Recorder.TraceWarning(null, "Trial no longer exists!");
            return Result.Ok;
        }

        var trial = ManagedTrial.Value;
        if (trial.IsConverted)
        {
            // ignore this signal, and do not produce another
            return Result.Ok;
        }

        if (trial.IsExpired)
        {
            // ignore the signal, and do not produce another
            return Result.Ok;
        }

        if (trial.IsExpirable)
        {
            // do not produce another signal
            return await ExpireManagedTrialAsync(interpreter, trial, onTrialExpired, onDispatchNextEvent);
        }

        // produce another signal
        return await onDispatchNextSignal(this);
    }

#if TESTINGONLY
    public void TestingOnly_SetManagedTrial(TrialTimeline trialTimeline)
    {
        ManagedTrial = trialTimeline;
    }
#endif

    /// <summary>
    ///     Dispatches the first event in the track corresponding to the current state of the trial
    /// </summary>
    private async Task<Result<Error>> DispatchFirstScheduledTrackEventInternalAsync(TrialTimeline trial,
        TrialEventSchedule schedule, DispatchTrialEventAction onDispatchEvent)
    {
        if (schedule.Events.HasNone())
        {
            Recorder.TraceWarning(null, "Trial has a schedule, but Schedule contains no items to dispatch!");
            return Result.Ok;
        }

        var firstTrackEvent = schedule
            .FilterEventTrack(trial.Status)
            .FirstOrDefault();
        if (firstTrackEvent.Exists())
        {
            return await onDispatchEvent(this, firstTrackEvent, trial.StartedAt);
        }

        return Result.Ok;
    }
}