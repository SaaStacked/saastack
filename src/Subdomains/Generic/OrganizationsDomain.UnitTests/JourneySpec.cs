using Common;
using Domain.Shared;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class JourneySpec
{
    [Fact]
    public void WhenEmpty_ThenEmpty()
    {
        var result = Journey.Empty;

        result.Steps.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithStep_ThenAssigned()
    {
        var step = Step.Create("astepid", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
                StringNameValues.Empty)
            .Value;

        var result = Journey.Create([step]);

        result.Should().BeSuccess();
        result.Value.Steps.Should().ContainSingle().Which.Should().Be(step);
    }

    [Fact]
    public void WhenAppendAnotherStep_ThenAppended()
    {
        var step1 = Step.Create("astepid1", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
                StringNameValues.Empty)
            .Value;
        var step2 = Step.Create("astepid2", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
                StringNameValues.Empty)
            .Value;

        var journey = Journey.Create([step1]).Value;

        var result = journey.AppendNextStep(step2);

        result.Should().BeSuccess();
        result.Value.Steps.Should().ContainInOrder(step1, step2);
    }

    [Fact]
    public void WhenHasAnyAndNone_ThenReturnsFalse()
    {
        var result = Journey.Empty.HasAny();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasAnyAndSome_ThenReturnsTrue()
    {
        var step = Step.Create("astepid", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
                StringNameValues.Empty)
            .Value;
        var journey = Journey.Create([step]).Value;

        var result = journey.HasAny();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLastAndNone_ThenReturnsNull()
    {
        var result = Journey.Empty.Last();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenReplaceLastStepAndNoSteps_ThenReturnsError()
    {
        var step = Step.Create("astepid", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
                StringNameValues.Empty)
            .Value;

        var result = Journey.Empty.ReplaceLastStep(step);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.Journey_ReplaceLastStep_NoSteps);
    }

    [Fact]
    public void WhenReplaceLastStep_ThenReturnsLast()
    {
        var step1 = Step.Create("astepid1", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
                StringNameValues.Empty)
            .Value;
        var step2 = Step.Create("astepid2", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
                StringNameValues.Empty)
            .Value;
        var step3 = Step.Create("astepid3", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
                StringNameValues.Empty)
            .Value;
        var step4 = Step.Create("astepid4", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
                StringNameValues.Empty)
            .Value;

        var journey = Journey.Create([step1, step2, step3]).Value;

        var result = journey.ReplaceLastStep(step4);

        result.Should().BeSuccess();
        result.Value.Steps.Should().ContainInOrder(step1, step2, step4);
    }
}