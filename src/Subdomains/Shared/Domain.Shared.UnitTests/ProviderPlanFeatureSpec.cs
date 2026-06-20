using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class ProviderPlanFeatureSpec
{
    [Fact]
    public void WhenCreateWithInvalidDescription_ThenReturnsError()
    {
        var result = ProviderPlanFeature.Create("^aninvaliddescription^");

        result.Should().BeError(ErrorCode.Validation, Resources.FeatureDefinition_InvalidDescription);
    }

    [Fact]
    public void WhenCreateWithExcluded_ThenAssigns()
    {
        var result = ProviderPlanFeature.Create("adescription", false);

        result.Should().BeSuccess();
        result.Value.Description.Should().Be("adescription");
        result.Value.IsIncluded.Should().BeFalse();
    }

    [Fact]
    public void WhenCreate_ThenAssigns()
    {
        var result = ProviderPlanFeature.Create("adescription");

        result.Should().BeSuccess();
        result.Value.Description.Should().Be("adescription");
        result.Value.IsIncluded.Should().BeTrue();
    }
}