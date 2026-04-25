using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

/// <summary>
///     A trial timeline follows the lifecycle of a provider subscription, and does not end.
///     The state machine always starts in the <see cref="TrialStatus.Active" />,
///     then either moves to: <see cref="TrialStatus.Converted" /> or to <see cref="TrialStatus.Expired" />
///     If expired, then possibly (later) to <see cref="TrialStatus.Converted" />
///     These state changes are a directed acyclical graph (DAG).
///     [Active] ----convert----> [Converted]
///     |
///     +-----expire------> [Expired] ----convert----> [Converted]
/// </summary>
public sealed class TrialTimeline : ValueObjectBase<TrialTimeline>
{
    public static Result<TrialTimeline, Error> Create(DateTime startsAt, int durationDays)
    {
        var now = DateTime.UtcNow;
        if (startsAt.IsInvalidParameter(time => time.IsBefore(now), nameof(startsAt),
                Resources.TrialTimeline_StartsAtInFuture, out var error1))
        {
            return error1;
        }

        if (durationDays.IsInvalidParameter(duration => duration > 0,
                nameof(durationDays),
                Resources.TrialTimeline_InvalidDuration, out var error2))
        {
            return error2;
        }

        return new TrialTimeline(startsAt, durationDays, Optional<DateTime>.None, Optional<DateTime>.None);
    }

    private TrialTimeline(DateTime startedAt, int durationDays, Optional<DateTime> convertedAt,
        Optional<DateTime> expiredAt)
    {
        StartedAt = startedAt.ToNearestHour(); // Round down to the present hour
        DurationDays = durationDays;
        ConvertedAt = convertedAt.HasValue
            ? convertedAt.Value.ToNearestMinute() //Rounded down to the last previous minute
            : Optional<DateTime>.None;
        ExpiredAt = expiredAt.HasValue
            ? expiredAt.Value.ToNearestMinute() //Rounded down to the last previous minute
            : Optional<DateTime>.None;
    }

    /// <summary>
    ///     Returns the date and time (to the nearest minute) that the trial was converted
    /// </summary>
    public Optional<DateTime> ConvertedAt { get; }

    /// <summary>
    ///     Returns the number of days the trial is active
    /// </summary>
    public int DurationDays { get; }

    /// <summary>
    ///     Returns the date and time (to the nearest minute) that the trial actual was expired
    /// </summary>
    public Optional<DateTime> ExpiredAt { get; private set; }

    /// <summary>
    ///     Returns the date and time (to the nearest hour) that the trial should expire
    /// </summary>
    public DateTime ExpiryDueAt => StartedAt.AddDays(DurationDays);

    /// <summary>
    ///     Whether the trial has been converted
    /// </summary>
    public bool IsConverted => ConvertedAt.HasValue;

    /// <summary>
    ///     Whether the trial reached its expiry date (in time)
    /// </summary>
    public bool IsExpirable => !IsExpired
                               && !IsConverted
                               && DateTime.UtcNow.ToNearestMinute().IsAfter(ExpiryDueAt);

    /// <summary>
    ///     Whether the trial expired
    /// </summary>
    public bool IsExpired => ExpiredAt.HasValue;

    /// <summary>
    ///     Returns the date and time (to the nearest hour) that the trial started
    /// </summary>
    public DateTime StartedAt { get; }

    /// <summary>
    ///     Returns the current status of the trial
    /// </summary>
    public TrialStatus Status
    {
        get
        {
            if (IsConverted)
            {
                return TrialStatus.Converted;
            }

            if (IsExpired)
            {
                return TrialStatus.Expired;
            }

            return TrialStatus.Active;
        }
    }

    [UsedImplicitly]
    public static ValueObjectFactory<TrialTimeline> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new TrialTimeline(
                parts[0].Value.FromIso8601(),
                parts[1].Value.ToInt(),
                parts[2].ToOptional(val => val.FromIso8601()),
                parts[3].ToOptional(val => val.FromIso8601())
            );
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [StartedAt, DurationDays, ConvertedAt, ExpiredAt];
    }

    public Result<TrialTimeline, Error> ConvertTrial(DateTime? convertedAt = null)
    {
        if (IsConverted)
        {
            return Error.PreconditionViolation(Resources.TrialTimeline_AlreadyConverted);
        }

        return new TrialTimeline(StartedAt, DurationDays, convertedAt ?? DateTime.UtcNow, ExpiredAt);
    }

    public Result<TrialTimeline, Error> ExpireTrial()
    {
        if (IsConverted)
        {
            return Error.PreconditionViolation(Resources.TrialTimeline_AlreadyConverted);
        }

        if (IsExpired)
        {
            return Error.PreconditionViolation(Resources.TrialTimeline_AlreadyExpired);
        }

        if (!IsExpirable)
        {
            return Error.PreconditionViolation(Resources.TrialTimeline_ExpiryNotDue);
        }

        ExpiredAt = DateTime.UtcNow.ToNearestMinute();

        return new TrialTimeline(StartedAt, DurationDays, ConvertedAt, ExpiredAt);
    }

    [SkipImmutabilityCheck]
    public bool IsEventApplyToTrialState(TrialScheduledEvent @event)
    {
        return @event.IsEventApplyToTrialState(Status);
    }

#if TESTINGONLY
    public Result<TrialTimeline, Error> TestingOnly_FastForwardToExpiry()
    {
        if (IsConverted)
        {
            return Error.PreconditionViolation(Resources.TrialTimeline_AlreadyConverted);
        }

        var offset = TimeSpan.FromDays(DurationDays);
        var startedAt = StartedAt.Subtract(offset);
        var convertedAt = ConvertedAt.HasValue
            ? ConvertedAt.Value.Subtract(offset).ToOptional()
            : Optional<DateTime>.None;
        var expiredAt = ExpiredAt.HasValue
            ? ExpiredAt.Value.Subtract(offset).ToOptional()
            : Optional<DateTime>.None;

        return new TrialTimeline(startedAt, DurationDays, convertedAt, expiredAt);
    }
#endif
}

public enum TrialStatus
{
    Active = 0,
    Converted = 1,
    Expired = 2
}