using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderPlanFeatureSectionSpec
{
    [Fact]
    public void WhenCreateWithInvalidName_ThenReturnsError()
    {
        var result = ProviderPlanFeatureSection.Create("^aninvalidname^", []);

        result.Should().BeError(ErrorCode.Validation, Resources.ProviderPlanFeatureSection_InvalidName);
    }

    [Fact]
    public void WhenCreateWithEmptyItems_ThenReturnsError()
    {
        var result = ProviderPlanFeatureSection.Create("aname", []);

        result.Should().BeError(ErrorCode.Validation, Resources.ProviderPlanFeatureSection_EmptyItems);
    }

    [Fact]
    public void WhenCreate_ThenAssigns()
    {
        var feature1 = ProviderPlanFeature.Create("adescription1").Value;
        var feature2 = ProviderPlanFeature.Create("adescription2").Value;
        var feature3 = ProviderPlanFeature.Create("adescription3").Value;

        var result = ProviderPlanFeatureSection.Create("aname", new List<ProviderPlanFeature>
        {
            feature1, feature2, feature3
        });

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
        result.Value.Features.Items.Count.Should().Be(3);
        result.Value.Features.Items[0].Should().Be(feature1);
        result.Value.Features.Items[1].Should().Be(feature2);
        result.Value.Features.Items[2].Should().Be(feature3);
    }
}