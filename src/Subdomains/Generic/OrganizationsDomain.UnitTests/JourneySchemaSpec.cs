using Common;
using FluentAssertions;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class JourneySchemaSpec
{
    [Fact]
    public void WhenEmpty_ThenReturnsEmpty()
    {
        var result = JourneySchema.Empty;

        result.Steps.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithEmptyDictionary_ThenReturnsJourney()
    {
        var result = JourneySchema.Create(new Dictionary<string, StepSchema>());

        result.IsSuccessful.Should().BeTrue();
        result.Value.Steps.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithSteps_ThenReturnsJourney()
    {
        var step1 = StepSchema.Create(
            "astepid1",
            OnboardingStepType.Normal,
            "atitle1",
            Optional<string>.None,
            "astepid2",
            [],
            10,
            new Dictionary<string, string>()).Value;

        var step2 = StepSchema.Create(
            "astepid2",
            OnboardingStepType.Normal,
            "atitle2",
            Optional<string>.None,
            "anendstepid",
            [],
            20,
            new Dictionary<string, string>()).Value;

        var result = JourneySchema.Create(new Dictionary<string, StepSchema>
        {
            { "astepid1", step1 },
            { "astepid2", step2 }
        });

        result.IsSuccessful.Should().BeTrue();
        result.Value.Steps.Should().HaveCount(2);
        result.Value.Steps["astepid1"].Should().Be(step1);
        result.Value.Steps["astepid2"].Should().Be(step2);
    }

    [Fact]
    public void WhenGetWithExistingStepId_ThenReturnsStep()
    {
        var step = StepSchema.Create("astepid1", OnboardingStepType.Normal, "atitle1", Optional<string>.None,
            "nextstep", [], 10, new Dictionary<string, string>()).Value;
        var journey = JourneySchema.Create(new Dictionary<string, StepSchema> { { "astepid1", step } }).Value;

        var result = journey.Get("astepid1");

        result.Should().Be(step);
    }

    [Fact]
    public void WhenTryGetValueWithExistingStepId_ThenReturnsTrue()
    {
        var step = StepSchema.Create("astepid1", OnboardingStepType.Normal, "atitle1", Optional<string>.None,
            "nextstep", [], 10, new Dictionary<string, string>()).Value;
        var steps = new Dictionary<string, StepSchema> { { "astepid1", step } };
        var journey = JourneySchema.Create(steps).Value;

        var result = journey.TryGetValue("astepid1", out var found);

        result.Should().BeTrue();
        found.Should().Be(step);
    }

    [Fact]
    public void WhenTryGetValueWithNonExistingStepId_ThenReturnsFalse()
    {
        var journey = JourneySchema.Create(new Dictionary<string, StepSchema>()).Value;

        var result = journey.TryGetValue("nonexistent", out var found);

        result.Should().BeFalse();
        found.Should().BeNull();
    }
}