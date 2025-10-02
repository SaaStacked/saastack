﻿using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared.Cars;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class UnavailabilitySpec
{
    private readonly DateTime _end;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly DateTime _start;
    private readonly Unavailability _unavailability;

    public UnavailabilitySpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _start = DateTime.UtcNow.ToNearestSecond();
        _end = _start.AddHours(1);
        _unavailability = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok).Value;
    }

    [Fact]
    public void WhenInitialized_ThenInitialized()
    {
        var timeSlot = TimeSlot.Create(_start, _end).Value;
        var causedBy = CausedBy.Create(UnavailabilityCausedBy.Maintenance, Optional<string>.None).Value;

        _unavailability.RaiseChangeEvent(Events.UnavailabilitySlotAdded("acarid".ToId(), "anorganizationid".ToId(),
            timeSlot, causedBy));

        _unavailability.Id.Should().Be("anid".ToId());
        _unavailability.OrganizationId.Should().Be("anorganizationid".ToId());
        _unavailability.CarId.Should().Be("acarid".ToId());
        _unavailability.Slot.Should().Be(timeSlot);
        _unavailability.CausedBy.Should().Be(causedBy);
    }

    [Fact]
    public void WhenOverlapsAndNotAssigned_ThenReturnsError()
    {
        var result = _unavailability.Overlaps(TimeSlot.Create(_end, _end.AddHours(1)).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UnavailabilityEntity_NotAssigned);
    }

    [Fact]
    public void WhenOverlapsAndNotOverlapping_ThenReturnsFalse()
    {
#if TESTINGONLY
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
#endif
        var slot = TimeSlot.Create(_end, _end.AddHours(1)).Value;

        var result = _unavailability.Overlaps(slot).Value;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenOverlapsAndOverlapping_ThenReturnsTrue()
    {
#if TESTINGONLY
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
#endif
        var slot = TimeSlot.Create(_start.SubtractHours(1), _end.AddHours(1)).Value;

        var result = _unavailability.Overlaps(slot).Value;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsDifferentCauseAndHasNoCausedByInEither_ThenReturnsFalse()
    {
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsDifferentCauseAndHasNoCausedInSource_ThenReturnsTrue()
    {
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
#if TESTINGONLY
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
#endif

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsDifferentCauseAndHasNoCausedInOther_ThenReturnsTrue()
    {
#if TESTINGONLY
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
#endif
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsDifferentCauseAndHaveSameCausesAndNoReferences_ThenReturnsFalse()
    {
#if TESTINGONLY
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
#endif
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
#if TESTINGONLY
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
#endif

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsDifferentCauseAndHaveSameCausesAndDifferentReferences_ThenReturnsTrue()
    {
#if TESTINGONLY
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);
#endif
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
#if TESTINGONLY
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference2").Value);
#endif

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsDifferentCauseAndHaveDifferentCausesAndNullReference_ThenReturnsTrue()
    {
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
#if TESTINGONLY
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Maintenance, null).Value);
#endif

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsDifferentCauseAndHaveDifferentCausesAndSameReference_ThenReturnsTrue()
    {
#if TESTINGONLY
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference").Value);
#endif
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
#if TESTINGONLY
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value,
            CausedBy.Create(UnavailabilityCausedBy.Maintenance, "areference").Value);
#endif

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsDifferentCauseAndHaveDifferentCausesAndDifferentReference_ThenReturnsTrue()
    {
#if TESTINGONLY
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);
#endif
        var other = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok);
#if TESTINGONLY
        other.Value.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value,
            CausedBy.Create(UnavailabilityCausedBy.Maintenance, "areference2").Value);
#endif

        var result = _unavailability.IsDifferentCause(other.Value);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEnsureInvariantsAndNoDetails_ThenReturnsError()
    {
        var result = _unavailability.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.UnavailabilityEntity_NotAssigned);
    }

    [Fact]
    public void WhenEnsureInvariantsAndAssigned_ThenReturnsTrue()
    {
#if TESTINGONLY
        _unavailability.TestingOnly_Assign("acarid".ToId(), "anorganizationid".ToId(),
            TimeSlot.Create(_start, _end).Value, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);
#endif

        var result = _unavailability.EnsureInvariants();

        result.Should().BeSuccess();
    }
}