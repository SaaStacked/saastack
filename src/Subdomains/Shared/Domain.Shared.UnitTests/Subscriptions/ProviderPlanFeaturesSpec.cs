using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderPlanFeaturesSpec
{
    [Fact]
    public void WhenCreateWithEmptyItems_ThenReturnsError()
    {
        var result = ProviderPlanFeatures.Create([]);

        result.Should().BeError(ErrorCode.Validation, Resources.ProviderPlanFeatures_EmptyItems);
    }

    [Fact]
    public void WhenCreate_ThenAssigns()
    {
        var feature1 = ProviderPlanFeature.Create("adescription1").Value;
        var feature2 = ProviderPlanFeature.Create("adescription2").Value;
        var feature3 = ProviderPlanFeature.Create("adescription3").Value;

        var result = ProviderPlanFeatures.Create([feature1, feature2, feature3]);

        result.Should().BeSuccess();
        result.Value.Items[0].Should().Be(feature1);
        result.Value.Items[1].Should().Be(feature2);
        result.Value.Items[2].Should().Be(feature3);
    }
}