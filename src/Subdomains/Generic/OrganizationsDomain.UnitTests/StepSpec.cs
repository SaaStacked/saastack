using Common;
using Domain.Shared;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class StepSpec
{
    [Fact]
    public void WhenCreateWithEmptyStepId_ThenReturnsError()
    {
        var result = Step.Create(string.Empty, "atitle", 0, DateTime.UtcNow, Optional<DateTime>.None,
            StringNameValues.Empty);

        result.Should().BeError(ErrorCode.Validation, Resources.Step_InvalidStepId);
    }

    [Fact]
    public void WhenCreateWithEmptyTitle_ThenReturnsError()
    {
        var result = Step.Create("astepid", string.Empty, 0, DateTime.UtcNow, Optional<DateTime>.None,
            StringNameValues.Empty);

        result.Should().BeError(ErrorCode.Validation, Resources.Step_InvalidTitle);
    }

    [Fact]
    public void WhenCreateWithWeightLessThanZero_ThenReturnsError()
    {
        var result = Step.Create("astepid", "atitle", -1, DateTime.UtcNow, Optional<DateTime>.None,
            StringNameValues.Empty);

        result.Should().BeError(ErrorCode.Validation, Resources.Step_InvalidWeight);
    }

    [Fact]
    public void WhenCreateWithWeightGreaterThanHundred_ThenReturnsError()
    {
        var result = Step.Create("astepid", "atitle", 101, DateTime.UtcNow, Optional<DateTime>.None,
            StringNameValues.Empty);

        result.Should().BeError(ErrorCode.Validation, Resources.Step_InvalidWeight);
    }

    [Fact]
    public void WhenCreate_ThenAssigned()
    {
        var datum = DateTime.UtcNow;
        var values = StringNameValues.Create(new Dictionary<string, string>
        {
            { "aname", "avalue" }
        }).Value;

        var result = Step.Create("astepid", "atitle", 10, datum, Optional<DateTime>.None, values);

        result.Should().BeSuccess();
        result.Value.StepId.Should().Be("astepid");
        result.Value.Title.Should().Be("atitle");
        result.Value.Weight.Should().Be(10);
        result.Value.EnteredAt.Should().Be(datum);
        result.Value.Values.Should().Be(values);
    }

    [Fact]
    public void WhenChangeValuesWithNewValues_ThenReturnsNewPathSegment()
    {
        var datum1 = DateTime.UtcNow;
        var values1 = StringNameValues.Create(new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        }).Value;
        var values2 = StringNameValues.Create(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        }).Value;

        var result1 = Step.Create("astepid", "atitle", 10, datum1, Optional<DateTime>.None, values1).Value;

        var result = result1.ChangeValues(values2);

        result.Should().BeSuccess();
        result.Value.StepId.Should().Be("astepid");
        result.Value.Title.Should().Be("atitle");
        result.Value.Weight.Should().Be(10);
        result.Value.EnteredAt.Should().Be(datum1);
        result.Value.Values.Should().Be(values2);
    }
}