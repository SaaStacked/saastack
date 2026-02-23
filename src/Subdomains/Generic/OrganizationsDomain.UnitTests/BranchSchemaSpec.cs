using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class BranchSchemaSpec
{
    [Fact]
    public void WhenCreateWithEmptyId_ThenReturnsError()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue").Value;

        var result = BranchSchema.Create(string.Empty, "alabel", condition, "astepid");

        result.Should().BeError(ErrorCode.Validation, Resources.BranchSchema_InvalidId);
    }

    [Fact]
    public void WhenCreateWithEmptyLabel_ThenReturnsError()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue").Value;

        var result = BranchSchema.Create("abranchid", string.Empty, condition, "astepid");

        result.Should().BeError(ErrorCode.Validation, Resources.BranchSchema_InvalidLabel);
    }

    [Fact]
    public void WhenCreateWithEmptyNextStepId_ThenReturnsError()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue").Value;

        var result = BranchSchema.Create("abranchid", "alabel", condition, string.Empty);

        result.Should().BeError(ErrorCode.Validation, Resources.BranchSchema_InvalidNextStepId);
    }

    [Fact]
    public void WhenCreateWithInvalidNextStepId_ThenReturnsError()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue").Value;

        var result = BranchSchema.Create("abranchid", "alabel", condition, "^aninvalidstepid^");

        result.Should().BeError(ErrorCode.Validation, Resources.BranchSchema_InvalidNextStepId);
    }

    [Fact]
    public void WhenCreate_ThenReturnsBranch()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue").Value;

        var result = BranchSchema.Create("abranchid", "alabel", condition, "astepid");

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("abranchid");
        result.Value.Label.Should().Be("alabel");
        result.Value.Condition.Should().Be(condition);
        result.Value.NextStepId.Should().Be("astepid");
    }
}