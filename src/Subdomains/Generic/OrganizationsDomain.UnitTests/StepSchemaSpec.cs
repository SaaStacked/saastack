using Common;
using Common.Extensions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class StepSchemaSpec
{
    [Fact]
    public void WhenCreateWithEmptyId_ThenReturnsError()
    {
        var result = StepSchema.Create(
            string.Empty,
            OnboardingStepType.Normal,
            "atitle",
            Optional<string>.None,
            "astepid",
            [],
            10,
            new Dictionary<string, string>());

        result.Should().BeError(ErrorCode.Validation, Resources.StepSchema_InvalidId);
    }

    [Fact]
    public void WhenCreateWithEmptyTitle_ThenReturnsError()
    {
        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Normal,
            string.Empty,
            Optional<string>.None,
            "astepid",
            [],
            10,
            new Dictionary<string, string>());

        result.Should().BeError(ErrorCode.Validation, Resources.StepSchema_InvalidTitle);
    }

    [Fact]
    public void WhenCreateWithNegativeWeight_ThenReturnsError()
    {
        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Normal,
            "atitle",
            Optional<string>.None,
            "astepid",
            [],
            -1,
            new Dictionary<string, string>());

        result.Should().BeError(ErrorCode.Validation, Resources.StepSchema_InvalidWeight);
    }

    [Fact]
    public void WhenCreateWithWeightGreaterThan100_ThenReturnsError()
    {
        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Normal,
            "atitle",
            Optional<string>.None,
            "astepid",
            [],
            101,
            new Dictionary<string, string>());

        result.Should().BeError(ErrorCode.Validation, Resources.StepSchema_InvalidWeight);
    }

    [Fact]
    public void WhenCreateBranchStepWithoutBranches_ThenReturnsError()
    {
        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Branch,
            "atitle",
            Optional<string>.None,
            Optional<string>.None,
            [],
            10,
            new Dictionary<string, string>());

        result.Should().BeError(ErrorCode.Validation, Resources.StepSchema_BranchStepWithoutBranchDefinition);
    }

    [Fact]
    public void WhenCreateBranchStepWithNextStepId_ThenReturnsError()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue").Value;
        var branch = BranchSchema.Create("branch1", "alabel1", condition, "astepid").Value;

        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Branch,
            "atitle",
            Optional<string>.None,
            "anextStepid",
            [branch],
            10,
            new Dictionary<string, string>());

        result.Should().BeError(ErrorCode.Validation, Resources.StepSchema_BranchStepCannotHaveNextStepId);
    }

    [Fact]
    public void WhenCreateNonBranchStepWithBranches_ThenReturnsError()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue").Value;
        var branch = BranchSchema.Create("branch1", "alabel1", condition, "astepid").Value;

        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Normal,
            "atitle",
            Optional<string>.None,
            "astepid",
            [branch],
            10,
            new Dictionary<string, string>());

        result.Should().BeError(ErrorCode.Validation, Resources.StepSchema_NonBranchStepWithBranchDefinition);
    }

    [Fact]
    public void WhenCreateStartStepWithoutNextStepId_ThenReturnsError()
    {
        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Start,
            "atitle",
            Optional<string>.None,
            Optional<string>.None,
            [],
            10,
            new Dictionary<string, string>());

        result.Should().BeError(ErrorCode.Validation,
            Resources.StepSchema_NonTerminatingNodeWithoutNextStep.Format(OnboardingStepType.Start));
    }

    [Fact]
    public void WhenCreateStepStepWithoutNextStepId_ThenReturnsError()
    {
        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Normal,
            "atitle",
            Optional<string>.None,
            Optional<string>.None,
            [],
            10,
            new Dictionary<string, string>());

        result.Should().BeError(ErrorCode.Validation,
            Resources.StepSchema_NonTerminatingNodeWithoutNextStep.Format(OnboardingStepType.Normal));
    }

    [Fact]
    public void WhenCreateWithStartStep_ThenReturnsStepSchema()
    {
        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Start,
            "atitle",
            "adescription",
            "astepid",
            [],
            10,
            new Dictionary<string, string> { { "aname", "avalue" } });

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Type.Should().Be(OnboardingStepType.Start);
        result.Value.Title.Should().Be("atitle");
        result.Value.Description.Should().BeSome("adescription");
        result.Value.NextStepId.Should().BeSome("astepid");
        result.Value.Branches.Items.Should().BeEmpty();
        result.Value.Weight.Should().Be(10);
        result.Value.InitialValues.Items.Should().ContainKey("aname");
    }

    [Fact]
    public void WhenCreateWithStepStep_ThenReturnsStepSchema()
    {
        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.Normal,
            "atitle",
            Optional<string>.None,
            "astepid",
            [],
            20,
            new Dictionary<string, string>());

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Type.Should().Be(OnboardingStepType.Normal);
        result.Value.Title.Should().Be("atitle");
        result.Value.Description.Should().BeNone();
        result.Value.NextStepId.Should().BeSome("astepid");
        result.Value.Branches.Items.Should().BeEmpty();
        result.Value.Weight.Should().Be(20);
    }

    [Fact]
    public void WhenCreateWithBranchStep_ThenReturnsStepSchema()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue").Value;
        var branch = BranchSchema.Create("branch1", "alabel1", condition, "astepid").Value;

        var result = StepSchema.Create(
            "decision",
            OnboardingStepType.Branch,
            "atitle",
            Optional<string>.None,
            Optional<string>.None,
            [branch],
            5,
            new Dictionary<string, string>());

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("decision");
        result.Value.Type.Should().Be(OnboardingStepType.Branch);
        result.Value.Title.Should().Be("atitle");
        result.Value.NextStepId.Should().BeNone();
        result.Value.Branches.Items.Should().HaveCount(1);
        result.Value.Weight.Should().Be(5);
    }

    [Fact]
    public void WhenCreateWithEndStep_ThenReturnsStepSchema()
    {
        var result = StepSchema.Create(
            "anid",
            OnboardingStepType.End,
            "atitle",
            Optional<string>.None,
            Optional<string>.None,
            [],
            0,
            new Dictionary<string, string>());

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Type.Should().Be(OnboardingStepType.End);
        result.Value.Title.Should().Be("atitle");
        result.Value.NextStepId.Should().BeNone();
        result.Value.Branches.Items.Should().BeEmpty();
        result.Value.Weight.Should().Be(0);
    }
}