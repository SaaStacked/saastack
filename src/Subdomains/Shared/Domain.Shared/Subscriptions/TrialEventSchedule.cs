using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

/// <summary>
///     Schedule is a list of events, which should remain in order.
///     Events are grouped and processed within tracks. Events within the same track need to be in order.
/// </summary>
public sealed class TrialEventSchedule : SingleValueObjectBase<TrialEventSchedule, List<TrialScheduledEvent>>
{
    public static readonly TrialEventSchedule Empty = new([]);

    public static Result<TrialEventSchedule, Error> Create(List<TrialScheduledEvent> events)
    {
        var ids = events
            .GroupBy(x => x.Id)
            .Count(group => group.Count() > 1);
        if (ids > 0)
        {
            return Error.Validation(Resources.TrialEventSchedule_DuplicateEvents);
        }

        return new TrialEventSchedule(events);
    }

    private TrialEventSchedule(List<TrialScheduledEvent> events) : base(events)
    {
    }

    public IReadOnlyList<TrialScheduledEvent> Events => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<TrialEventSchedule> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            return new TrialEventSchedule(
                items
                    .Where(item => item.HasValue)
                    .Select(item => TrialScheduledEvent.Rehydrate()(item, container))
                    .ToList());
        };
    }

    public Result<TrialEventSchedule, Error> Append(TrialScheduledEvent @event)
    {
        return Create(Value.Append(@event).ToList());
    }

    /// <summary>
    ///     Filters events (by Track) according to the state of the trial
    /// </summary>
    [SkipImmutabilityCheck]
    public IReadOnlyList<TrialScheduledEvent> FilterEventTrack(TrialStatus status)
    {
        return Events
            .Where(evt => evt.IsEventApplyToTrialState(status))
            .ToList();
    }
}