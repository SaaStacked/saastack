using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class TrialEventScheduleSpec
{
    [Fact]
    public void WhenEmpty_ThenReturnsNone()
    {
        var result = TrialEventSchedule.Empty;

        result.Events.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithSingleEvent_ThenAssigned()
    {
        var @event = TrialScheduledEvent.Create(0, "anid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

        var result = TrialEventSchedule.Create([@event]);

        result.Should().BeSuccess();
        result.Value.Events.Should().ContainSingle().Which.Should().Be(@event);
    }

    [Fact]
    public void WhenCreateWithDuplicateEvents_ThenReturnsError()
    {
        var event1 = TrialScheduledEvent.Create(0, "anid", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty)
            .Value;
        var event2 = TrialScheduledEvent.Create(0, "anid", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty)
            .Value;

        var result = TrialEventSchedule.Create([event1, event2]);

        result.Should().BeError(ErrorCode.Validation, Resources.TrialEventSchedule_DuplicateEvents);
    }

    [Fact]
    public void WhenAppendAnotherEvent_ThenAppended()
    {
        var event1 = TrialScheduledEvent.Create(0, "anid1", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty)
            .Value;
        var event2 = TrialScheduledEvent.Create(0, "anid2", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty)
            .Value;

        var schedule = TrialEventSchedule.Create([event1]).Value;

        var result = schedule.Append(event2);

        result.Should().BeSuccess();
        result.Value.Events.Should().ContainInOrder(event1, event2);
    }
}