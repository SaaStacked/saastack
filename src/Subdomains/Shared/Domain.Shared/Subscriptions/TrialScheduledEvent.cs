using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

/// <summary>
///     Events are delivered in the order they appear in their repspective tracks, in the schedule
///     They are delivered one at a time, and spaced in time according to their relative <see cref="DelayInDays" />.
/// </summary>
public sealed class TrialScheduledEvent : ValueObjectBase<TrialScheduledEvent>
{
    public static Result<TrialScheduledEvent, Error> Create(int delayInDays, string id,
        TrialScheduledEventTrack appliesTo, TrialScheduledEventAction action,
        StringNameValues metadata)
    {
        if (delayInDays.IsInvalidParameter(delay => delay >= 0, nameof(delayInDays),
                Resources.TrialScheduleEvent_InvalidDelay, out var error1))
        {
            return error1;
        }

        if (id.IsInvalidParameter(i => i.HasValue(), nameof(id),
                Resources.TrialScheduleEvent_InvalidId, out var error2))
        {
            return error2;
        }

        return new TrialScheduledEvent(id, delayInDays, appliesTo, action, metadata);
    }

    private TrialScheduledEvent(string id, int delayInDays, TrialScheduledEventTrack track,
        TrialScheduledEventAction action,
        StringNameValues metadata)
    {
        Id = id;
        DelayInDays = delayInDays;
        Track = track;
        Action = action;
        Metadata = metadata;
    }

    public TrialScheduledEventAction Action { get; }

    public int DelayInDays { get; }

    public string Id { get; }

    public StringNameValues Metadata { get; }

    public TrialScheduledEventTrack Track { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<TrialScheduledEvent> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new TrialScheduledEvent(
                parts[0].Value,
                parts[1].Value.ToIntOrDefault(0),
                parts[2].Value.ToEnumOrDefault(TrialScheduledEventTrack.Active),
                parts[3].Value.ToEnumOrDefault(TrialScheduledEventAction.Notification),
                StringNameValues.Rehydrate()(parts[4], container));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Id, DelayInDays, Track, Action, Metadata];
    }

    [SkipImmutabilityCheck]
    public bool IsEventApplyToTrialState(TrialStatus status)
    {
        return Track switch
        {
            TrialScheduledEventTrack.Active => status == TrialStatus.Active,
            TrialScheduledEventTrack.Expired => status == TrialStatus.Expired,
            TrialScheduledEventTrack.Converted => status == TrialStatus.Converted,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

public enum TrialScheduledEventTrack
{
    Active = 0, // Before expiry, and NOT converted
    Expired = 1, // After expiry, and NOT converted yet
    Converted = 2 // Could be before or after expiry
}

public enum TrialScheduledEventAction
{
    Notification = 0
}

public enum TrialScheduledEndingReason
{
    NoMoreEvents = 0, // reached the end of the list (for current state)
    TrialMissing = 1, // trial does not exist anymore
    TrialScheduleRemoved = 2 // no schedule exists anymore
}