using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class BranchConditionSchemaSpec
{
    [Fact]
    public void WhenCreateWithEmptyField_ThenReturnsError()
    {
        var result = BranchConditionSchema.Create(BranchConditionOperator.Equals, string.Empty, "avalue");

        result.Should().BeError(ErrorCode.Validation, Resources.BranchConditionschema_InvalidName);
    }

    [Fact]
    public void WhenCreateWithNullValue_ThenReturnsError()
    {
        var result = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", null!);

        result.Should().BeError(ErrorCode.Validation, Resources.BranchConditionschema_InvalidValue);
    }

    [Fact]
    public void WhenCreateWithEmptyValue_ThenReturnsCondition()
    {
        var result = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", string.Empty);

        result.Should().BeSuccess();
        result.Value.Operator.Should().Be(BranchConditionOperator.Equals);
        result.Value.Field.Should().Be("afield");
        result.Value.Value.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreate_ThenReturnsCondition()
    {
        var result = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue");

        result.Should().BeSuccess();
        result.Value.Operator.Should().Be(BranchConditionOperator.Equals);
        result.Value.Field.Should().Be("afield");
        result.Value.Value.Should().Be("avalue");
    }

    [Fact]
    public void WhenEvaluateEqualsWithMatchingValue_ThenReturnsTrue()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "status", "active").Value;
        var values = new Dictionary<string, string> { { "status", "active" } };

        var result = condition.Evaluate(values);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEvaluateEqualsWithNonMatchingValue_ThenReturnsFalse()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "status", "active").Value;
        var values = new Dictionary<string, string> { { "status", "inactive" } };

        var result = condition.Evaluate(values);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEvaluateWithMissingField_ThenReturnsFalse()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, "status", "active").Value;
        var values = new Dictionary<string, string> { { "other", "avalue" } };

        var result = condition.Evaluate(values);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEvaluateContainsWithMatchingSubstring_ThenReturnsTrue()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Contains, "name", "john").Value;
        var values = new Dictionary<string, string> { { "name", "John Doe" } };

        var result = condition.Evaluate(values);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEvaluateContainsWithNonMatchingSubstring_ThenReturnsFalse()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Contains, "name", "jane").Value;
        var values = new Dictionary<string, string> { { "name", "John Doe" } };

        var result = condition.Evaluate(values);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEvaluateGreaterThanWithLargerValue_ThenReturnsTrue()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.GreaterThan, "age", "18").Value;
        var values = new Dictionary<string, string> { { "age", "25" } };

        var result = condition.Evaluate(values);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEvaluateGreaterThanWithSmallerValue_ThenReturnsFalse()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.GreaterThan, "age", "18").Value;
        var values = new Dictionary<string, string> { { "age", "15" } };

        var result = condition.Evaluate(values);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEvaluateGreaterThanWithEqualValue_ThenReturnsFalse()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.GreaterThan, "age", "18").Value;
        var values = new Dictionary<string, string> { { "age", "18" } };

        var result = condition.Evaluate(values);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEvaluateGreaterThanWithNonNumericValue_ThenReturnsFalse()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.GreaterThan, "age", "18").Value;
        var values = new Dictionary<string, string> { { "age", "not a number" } };

        var result = condition.Evaluate(values);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEvaluateLessThanWithSmallerValue_ThenReturnsTrue()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.LessThan, "age", "65").Value;
        var values = new Dictionary<string, string> { { "age", "30" } };

        var result = condition.Evaluate(values);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEvaluateLessThanWithLargerValue_ThenReturnsFalse()
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.LessThan, "age", "65").Value;
        var values = new Dictionary<string, string> { { "age", "70" } };

        var result = condition.Evaluate(values);

        result.Should().BeFalse();
    }
}