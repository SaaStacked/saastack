using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderPlanQuotasSpec
{
    [Fact]
    public void WhenCreateWithSingle_ThenCreates()
    {
        var quota = ProviderPlanQuota.Create("adescription", 2).Value;

        var result = ProviderPlanQuotas.Create("anid", quota);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(1);
        result.Value.Items["anid"].Should().Be(quota);
    }

    [Fact]
    public void WhenCreateMultipleWithEmpty_ThenReturnsError()
    {
        var result = ProviderPlanQuotas.Create(new Dictionary<string, ProviderPlanQuota>());

        result.Should().BeError(ErrorCode.Validation, Resources.ProviderPlanQuotas_EmptyItems);
    }

    [Fact]
    public void WhenCreateWithMultiple_ThenCreates()
    {
        var quota1 = ProviderPlanQuota.Create("adescription", 2).Value;
        var quota2 = ProviderPlanQuota.Create("adescription", 2).Value;
        var quota3 = ProviderPlanQuota.Create("adescription", 2).Value;

        var result = ProviderPlanQuotas.Create(new Dictionary<string, ProviderPlanQuota>
        {
            { "anid1", quota1 },
            { "anid2", quota2 },
            { "anid3", quota3 }
        });

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(3);
        result.Value.Items["anid1"].Should().Be(quota1);
        result.Value.Items["anid2"].Should().Be(quota2);
        result.Value.Items["anid3"].Should().Be(quota3);
    }
}