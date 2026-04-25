using Common;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderPlanSpec
{
    [Fact]
    public void WhenCreateWithEmptyPlanId_ThenReturnsError()
    {
        var result =
            ProviderPlan.Create(string.Empty, Optional<TrialTimeline>.None, BillingSubscriptionTier.Enterprise);

        result.Should().BeError(ErrorCode.Validation, Resources.ProviderPlan_InvalidPlanId);
    }

    [Fact]
    public void WhenCreate_ThenReturnsPlan()
    {
        var now = DateTime.UtcNow;
        var trial = TrialTimeline.Create(now, 1).Value;

        var result = ProviderPlan.Create("anid", trial, BillingSubscriptionTier.Enterprise);

        result.Should().BeSuccess();
        result.Value.PlanId.Should().Be("anid");
        result.Value.Trial.Should().Be(trial);
        result.Value.Tier.Should().Be(BillingSubscriptionTier.Enterprise);
    }
}