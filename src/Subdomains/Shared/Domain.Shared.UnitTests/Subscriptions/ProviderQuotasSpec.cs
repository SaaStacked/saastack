using Common;
using Common.Extensions;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests.Subscriptions;

[Trait("Category", "Unit")]
public class ProviderQuotasSpec
{
    [Fact]
    public void WhenCreateMultipleWithEmpty_ThenReturnsError()
    {
        var result = ProviderQuotas.Create(new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>());

        result.Should().BeError(ErrorCode.Validation, Resources.ProviderQuotas_EmptyItems);
    }

    [Fact]
    public void WhenCreateMultipleWithAnyUnsubscribed_ThenReturnsError()
    {
        var result = ProviderQuotas.Create(new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
        {
            {
                BillingSubscriptionTier.Unsubscribed,
                ProviderPlanQuotas.Create("anid", ProviderPlanQuota.Create("adescription").Value).Value
            }
        });

        result.Should().BeError(ErrorCode.Validation,
            Resources.ProviderQuotas_UnsubscribedItems.Format(BillingSubscriptionTier.Unsubscribed));
    }

    [Fact]
    public void WhenCreate_ThenCreates()
    {
        var quota = ProviderPlanQuota.Create("adescription", 2).Value;
        var quotas = ProviderPlanQuotas.Create("aquotaid", quota).Value;

        var result = ProviderQuotas.Create(new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
        {
            { BillingSubscriptionTier.Standard, quotas }
        });

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(1);
        result.Value.Items[BillingSubscriptionTier.Standard].Should().Be(quotas);
    }
}