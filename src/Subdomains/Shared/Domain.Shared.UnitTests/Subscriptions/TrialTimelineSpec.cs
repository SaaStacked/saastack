using Common;
using Common.Extensions;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using JetBrains.Annotations;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[UsedImplicitly]
public class TrialTimelineSpec
{
    [Trait("Category", "Unit")]
    public class GivenNoContext
    {
        [Fact]
        public void WhenCreateAndStartIsFuture_ThenReturnsError()
        {
            var future = DateTime.UtcNow.AddHours(1);

            var result = TrialTimeline.Create(future, 1);

            result.Should().BeError(ErrorCode.Validation, Resources.TrialTimeline_StartsAtInFuture);
        }

        [Fact]
        public void WhenCreateAndDurationIsNegative_ThenReturnsError()
        {
            var now = DateTime.UtcNow;

            var result = TrialTimeline.Create(now, -1);

            result.Should().BeError(ErrorCode.Validation, Resources.TrialTimeline_InvalidDuration);
        }

        [Fact]
        public void WhenCreateAndDurationIsZero_ThenReturnsError()
        {
            var now = DateTime.UtcNow;

            var result = TrialTimeline.Create(now, 0);

            result.Should().BeError(ErrorCode.Validation, Resources.TrialTimeline_InvalidDuration);
        }

        [Fact]
        public void WhenCreate_ThenReturnsTimeline()
        {
            var past = DateTime.UtcNow.SubtractSeconds(1);

            var result = TrialTimeline.Create(past, 1);

            result.Should().BeSuccess();
            result.Value.StartedAt.Should().Be(past.ToNearestHour());
            result.Value.DurationDays.Should().Be(1);
            result.Value.ConvertedAt.Should().BeNone();
            result.Value.IsConverted.Should().BeFalse();
            result.Value.ExpiryDueAt.Should().Be(past.ToNearestHour().AddDays(1));
            result.Value.ExpiredAt.Should().BeNone();
            result.Value.IsExpirable.Should().BeFalse();
            result.Value.IsExpired.Should().BeFalse();
            result.Value.Status.Should().Be(TrialStatus.Active);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenActiveBeforeExpiryDueTrial
    {
        private readonly DateTime _startedAt;
        private readonly TrialTimeline _trial;

        public GivenActiveBeforeExpiryDueTrial()
        {
            _startedAt = DateTime.UtcNow.SubtractSeconds(1);
            _trial = TrialTimeline.Create(_startedAt, 1).Value;
        }

        [Fact]
        public void ThenActive()
        {
            _trial.StartedAt.Should().Be(_startedAt.ToNearestHour());
            _trial.DurationDays.Should().Be(1);
            _trial.ConvertedAt.Should().BeNone();
            _trial.IsConverted.Should().BeFalse();
            _trial.ExpiryDueAt.Should().Be(_startedAt.ToNearestHour().AddDays(1));
            _trial.ExpiredAt.Should().BeNone();
            _trial.IsExpired.Should().BeFalse();
            _trial.IsExpirable.Should().BeFalse();
            _trial.Status.Should().Be(TrialStatus.Active);
        }

        [Fact]
        public void WhenConvertTrial_ThenReturnsConverted()
        {
            var result = _trial.ConvertTrial();

            result.Should().BeSuccess();
            result.Value.ConvertedAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
            result.Value.IsConverted.Should().BeTrue();
            result.Value.Status.Should().Be(TrialStatus.Converted);
        }

        [Fact]
        public void WhenExpireTrial_ThenReturnsError()
        {
            var result = _trial.ExpireTrial();

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.TrialTimeline_ExpiryNotDue);
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateForActiveTrack_ThenReturnsTrue()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateForConvertedTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Converted,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateForExpiredTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Expired,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenActiveAfterExpiryDueTrial
    {
        private readonly DateTime _startedAt;
        private readonly TrialTimeline _trial;

        public GivenActiveAfterExpiryDueTrial()
        {
            _startedAt = DateTime.UtcNow.SubtractDays(10);
            _trial = TrialTimeline.Create(_startedAt, 1).Value;
        }

        [Fact]
        public void ThenActiveAndExpirable()
        {
            _trial.StartedAt.Should().Be(_startedAt.ToNearestHour());
            _trial.DurationDays.Should().Be(1);
            _trial.ConvertedAt.Should().BeNone();
            _trial.IsConverted.Should().BeFalse();
            _trial.ExpiryDueAt.Should().Be(_startedAt.ToNearestHour().AddDays(1));
            _trial.ExpiredAt.Should().BeNone();
            _trial.IsExpired.Should().BeFalse();
            _trial.IsExpirable.Should().BeTrue();
            _trial.Status.Should().Be(TrialStatus.Active);
        }

        [Fact]
        public void WhenConvertTrial_ThenReturnsConverted()
        {
            var result = _trial.ConvertTrial();

            result.Should().BeSuccess();
            result.Value.ConvertedAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
            result.Value.IsConverted.Should().BeTrue();
            result.Value.Status.Should().Be(TrialStatus.Converted);
        }

        [Fact]
        public void WhenExpireTrial_ThenReturnsExpired()
        {
            var result = _trial.ExpireTrial();

            result.Should().BeSuccess();
            result.Value.IsExpirable.Should().BeFalse();
            result.Value.IsExpired.Should().BeTrue();
            result.Value.ExpiredAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
            result.Value.Status.Should().Be(TrialStatus.Expired);
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateForActiveTrack_ThenReturnsTrue()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateForConvertedTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Converted,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateForExpiredTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Expired,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenConvertedBeforeExpiryDueTrial
    {
        private readonly DateTime _startedAt;
        private readonly TrialTimeline _trial;

        public GivenConvertedBeforeExpiryDueTrial()
        {
            _startedAt = DateTime.UtcNow.SubtractSeconds(1);
            var trial = TrialTimeline.Create(_startedAt, 1).Value;
            _trial = trial.ConvertTrial().Value;
        }

        [Fact]
        public void ThenConverted()
        {
            _trial.StartedAt.Should().Be(_startedAt.ToNearestHour());
            _trial.DurationDays.Should().Be(1);
            _trial.ConvertedAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
            _trial.IsConverted.Should().BeTrue();
            _trial.ExpiryDueAt.Should().Be(_startedAt.ToNearestHour().AddDays(1));
            _trial.ExpiredAt.Should().BeNone();
            _trial.IsExpired.Should().BeFalse();
            _trial.IsExpirable.Should().BeFalse();
            _trial.Status.Should().Be(TrialStatus.Converted);
        }

        [Fact]
        public void WhenConvertTrial_ThenReturnsError()
        {
            var result = _trial.ConvertTrial();

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.TrialTimeline_AlreadyConverted);
        }

        [Fact]
        public void WhenExpireTrial_ThenReturnsError()
        {
            var result = _trial.ExpireTrial();

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.TrialTimeline_AlreadyConverted);
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsConvertedForActiveTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsConvertedForConvertedTrack_ThenReturnsTrue()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Converted,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsConvertedForExpiredTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Expired,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenConvertedAfterExpiryDueTrial
    {
        private readonly DateTime _startedAt;
        private readonly TrialTimeline _trial;

        public GivenConvertedAfterExpiryDueTrial()
        {
            _startedAt = DateTime.UtcNow.SubtractDays(10);
            var trial = TrialTimeline.Create(_startedAt, 1).Value;
            _trial = trial.ConvertTrial().Value;
        }

        [Fact]
        public void ThenConverted()
        {
            _trial.StartedAt.Should().Be(_startedAt.ToNearestHour());
            _trial.DurationDays.Should().Be(1);
            _trial.ConvertedAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
            _trial.IsConverted.Should().BeTrue();
            _trial.ExpiryDueAt.Should().Be(_startedAt.ToNearestHour().AddDays(1));
            _trial.ExpiredAt.Should().BeNone();
            _trial.IsExpired.Should().BeFalse();
            _trial.IsExpirable.Should().BeFalse();
            _trial.Status.Should().Be(TrialStatus.Converted);
        }

        [Fact]
        public void WhenConvertTrial_ThenReturnsError()
        {
            var result = _trial.ConvertTrial();

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.TrialTimeline_AlreadyConverted);
        }

        [Fact]
        public void WhenExpireTrial_ThenReturnsError()
        {
            var result = _trial.ExpireTrial();

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.TrialTimeline_AlreadyConverted);
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsConvertedForActiveTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsConvertedForConvertedTrack_ThenReturnsTrue()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Converted,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsConvertedForExpiredTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Expired,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenConvertedAfterExpiredTrial
    {
        private readonly DateTime _startedAt;
        private readonly TrialTimeline _trial;

        public GivenConvertedAfterExpiredTrial()
        {
            _startedAt = DateTime.UtcNow.SubtractDays(10);
            var trial = TrialTimeline.Create(_startedAt, 1).Value;
            _trial = trial.ExpireTrial().Value;
            _trial = trial.ConvertTrial().Value;
        }

        [Fact]
        public void ThenExpiredAndConverted()
        {
            _trial.StartedAt.Should().Be(_startedAt.ToNearestHour());
            _trial.DurationDays.Should().Be(1);
            _trial.ConvertedAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
            _trial.IsConverted.Should().BeTrue();
            _trial.ExpiryDueAt.Should().Be(_startedAt.ToNearestHour().AddDays(1));
            _trial.ExpiredAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
            _trial.IsExpired.Should().BeTrue();
            _trial.IsExpirable.Should().BeFalse();
            _trial.Status.Should().Be(TrialStatus.Converted);
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsConvertedForActiveTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsConvertedForConvertedTrack_ThenReturnsTrue()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Converted,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeTrue();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsConvertedForExpiredTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Expired,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenExpiredNotConvertedTrial
    {
        private readonly DateTime _startedAt;
        private readonly TrialTimeline _trial;

        public GivenExpiredNotConvertedTrial()
        {
            _startedAt = DateTime.UtcNow.SubtractDays(10);
            var trial = TrialTimeline.Create(_startedAt, 1).Value;
            _trial = trial.ExpireTrial().Value;
        }

        [Fact]
        public void ThenExpired()
        {
            _trial.StartedAt.Should().Be(_startedAt.ToNearestHour());
            _trial.DurationDays.Should().Be(1);
            _trial.ConvertedAt.Should().BeNone();
            _trial.IsConverted.Should().BeFalse();
            _trial.ExpiryDueAt.Should().Be(_startedAt.ToNearestHour().AddDays(1));
            _trial.ExpiredAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
            _trial.IsExpired.Should().BeTrue();
            _trial.IsExpirable.Should().BeFalse();
            _trial.Status.Should().Be(TrialStatus.Expired);
        }

        [Fact]
        public void WhenConvertTrial_ThenReturnsConverted()
        {
            var result = _trial.ConvertTrial();

            result.Should().BeSuccess();
            result.Value.ConvertedAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
            result.Value.Status.Should().Be(TrialStatus.Converted);
            result.Value.IsConverted.Should().BeTrue();
            result.Value.IsExpirable.Should().BeFalse();
            result.Value.IsExpired.Should().BeTrue();
        }

        [Fact]
        public void WhenExpireTrial_ThenReturnsError()
        {
            var result = _trial.ExpireTrial();

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.TrialTimeline_AlreadyExpired);
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsExpiredForActiveTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsExpiredForConvertedTrack_ThenReturnsFalse()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Converted,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeFalse();
        }

        [Fact]
        public void WhenDoesTrialEventApplyToTrialStateAndIsExpiredForExpiredTrack_ThenReturnsTrue()
        {
            var @event = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Expired,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

            var result = _trial.IsEventApplyToTrialState(@event);

            result.Should().BeTrue();
        }
    }
}