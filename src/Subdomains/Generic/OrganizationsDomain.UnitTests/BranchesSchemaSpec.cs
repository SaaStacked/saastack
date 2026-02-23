using FluentAssertions;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class BranchesSchemaSpec
{
    [Fact]
    public void WhenCreateWithEmptyList_ThenReturnsBranchesSchema()
    {
        var result = BranchesSchema.Create([]);

        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithMultipleBranches_ThenReturnsBranches()
    {
        var condition1 = BranchConditionSchema.Create(BranchConditionOperator.Equals, "field", "value1").Value;
        var branch1 = BranchSchema.Create("branch1", "Branch 1", condition1, "nextstep1").Value;

        var condition2 = BranchConditionSchema.Create(BranchConditionOperator.Equals, "field", "value2").Value;
        var branch2 = BranchSchema.Create("branch2", "Branch 2", condition2, "nextstep2").Value;

        var result = BranchesSchema.Create([branch1, branch2]);

        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Should().Be(branch1);
        result.Value.Items[1].Should().Be(branch2);
    }
}