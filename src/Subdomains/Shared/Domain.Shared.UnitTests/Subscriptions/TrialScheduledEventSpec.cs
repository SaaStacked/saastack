using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class TrialScheduledEventSpec
{
    [Fact]
    public void WhenCreateAndDelayIsNegative_ThenReturnsError()
    {
        var metadata = StringNameValues.Create(new Dictionary<string, string>
        {
            { "aname", "avalue" }
        }).Value;
        var result = TrialScheduledEvent.Create(-1, "anid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, metadata);

        result.Should().BeError(ErrorCode.Validation, Resources.TrialScheduleEvent_InvalidDelay);
    }

    [Fact]
    public void WhenCreateAndIdIsEmpty_ThenReturnsError()
    {
        var metadata = StringNameValues.Create(new Dictionary<string, string>
        {
            { "aname", "avalue" }
        }).Value;
        var result = TrialScheduledEvent.Create(0, string.Empty, TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, metadata);

        result.Should().BeError(ErrorCode.Validation, Resources.TrialScheduleEvent_InvalidId);
    }

    [Fact]
    public void WhenCreate_ThenReturnsItem()
    {
        var metadata = StringNameValues.Create(new Dictionary<string, string>
        {
            { "aname", "avalue" }
        }).Value;
        var result = TrialScheduledEvent.Create(0, "anid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, metadata);

        result.Should().BeSuccess();
        result.Value.Track.Should().Be(TrialScheduledEventTrack.Active);
        result.Value.Action.Should().Be(TrialScheduledEventAction.Notification);
        result.Value.Metadata.Should().Be(metadata);
    }
}