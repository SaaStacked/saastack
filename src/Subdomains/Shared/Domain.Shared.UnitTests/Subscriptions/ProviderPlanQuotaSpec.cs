using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderPlanQuotaSpec
{
    [Fact]
    public void WhenCreateWithInvalidDescription_ThenReturnsError()
    {
        var result = ProviderPlanQuota.Create("^aninvaliddescription^");

        result.Should().BeError(ErrorCode.Validation, Resources.ProviderPlanQuota_InvalidDescription);
    }

    [Fact]
    public void WhenCreateAndNoLimit_ThenAssigns()
    {
        var result = ProviderPlanQuota.Create("adescription");

        result.Should().BeSuccess();
        result.Value.Description.Should().Be("adescription");
        result.Value.Limit.Should().Be(-1);
    }

    [Fact]
    public void WhenCreateAndLimit_ThenAssigns()
    {
        var result = ProviderPlanQuota.Create("adescription", 3);

        result.Should().BeSuccess();
        result.Value.Description.Should().Be("adescription");
        result.Value.Limit.Should().Be(3);
    }
}