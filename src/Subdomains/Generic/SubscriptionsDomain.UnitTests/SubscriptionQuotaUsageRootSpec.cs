using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace SubscriptionsDomain.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionQuotaUsageRootSpec
{
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly SubscriptionQuotaUsageRoot _usage;

    public SubscriptionQuotaUsageRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());

        _usage = SubscriptionQuotaUsageRoot.Create(_recorder.Object, _identifierFactory.Object,
            "asubscriptionid".ToId(), "anowningentityid".ToId(), "aprovidername").Value;
    }

    [Fact]
    public void WhenCreateWithInvalidProviderName_ThenReturnsError()
    {
        var result = SubscriptionQuotaUsageRoot.Create(_recorder.Object, _identifierFactory.Object,
            "asubscriptionid".ToId(), "anowningentityid".ToId(), "^aninvalidprovidername^");

        result.Should().BeError(ErrorCode.Validation, Resources.SubscriptionQuotaUsageRoot_InvalidProviderName);
    }

    [Fact]
    public void WhenCreate_ThenAssigned()
    {
        var result = SubscriptionQuotaUsageRoot.Create(_recorder.Object, _identifierFactory.Object,
            "asubscriptionid".ToId(), "anowningentityid".ToId(), "aprovidername");

        result.Should().BeSuccess();
        result.Value.SubscriptionId.Should().Be("asubscriptionid".ToId());
        result.Value.ProviderName.Should().Be("aprovidername");
        result.Value.OwningEntityId.Should().Be("anowningentityid".ToId());
        result.Value.Period.Should().Be(BillingSubscriptionQuotaPeriod.Eternity);
        result.Value.Limit.Should().Be(-1);
        result.Value.Total.Should().Be(0);
        result.Value.LastResetAt.Should().BeNone();
        result.Value.QuotaId.Should().BeNone();
        result.Value.SubscriptionTier.Should().Be(BillingSubscriptionTier.Unsubscribed);
    }

    [Fact]
    public void WhenConfigureAndNothingHasChanged_ThenDoesNothing()
    {
        var quota = ProviderPlanQuota.Create("adescription", 2).Value;
        _usage.Configure(BillingSubscriptionTier.Enterprise, "aquotaid", quota);
        _usage.ClearChanges();

        var result = _usage.Configure(BillingSubscriptionTier.Enterprise, "aquotaid", quota);

        result.Should().BeSuccess();
        _usage.Events.Should().BeEmpty();
    }

    [Fact]
    public void WhenConfigure_ThenConfigures()
    {
        var quota = ProviderPlanQuota.Create("adescription", 2).Value;

        var result = _usage.Configure(BillingSubscriptionTier.Enterprise, "aquotaid", quota);

        result.Should().BeSuccess();
        _usage.Period.Should().Be(BillingSubscriptionQuotaPeriod.Eternity);
        _usage.Limit.Should().Be(2);
        _usage.Total.Should().Be(0);
        _usage.LastResetAt.Should().BeNear(DateTime.UtcNow);
        _usage.QuotaId.Should().Be("aquotaid");
        _usage.SubscriptionTier.Should().Be(BillingSubscriptionTier.Enterprise);
    }

    [Fact]
    public void WhenSetTotalWithNegativeNumber_ThenReturnsError()
    {
        var result = _usage.SetTotal(-1);

        result.Should().BeError(ErrorCode.Validation, Resources.SubscriptionQuotaUsageRoot_InvalidTotal);
    }

    [Fact]
    public void WhenSetTotalAndNothingHasChanged_ThenDoesNothing()
    {
        _usage.SetTotal(2);
        _usage.ClearChanges();

        var result = _usage.SetTotal(2);

        result.Should().BeSuccess();
        _usage.Events.Should().BeEmpty();
    }

    [Fact]
    public void WhenSetTotalAndExceedsLimit_ThenReturnsError()
    {
        var quota = ProviderPlanQuota.Create("adescription", 2).Value;
        _usage.Configure(BillingSubscriptionTier.Standard, "aquotaid", quota);

        var result = _usage.SetTotal(3);

        result.Should().BeError(ErrorCode.FeatureViolation,
            Resources.SubscriptionsQuotaUsageRoot_LimitExceeded.Format("aquotaid", 2,
                BillingSubscriptionTier.Standard));
    }

    [Fact]
    public void WhenSetTotal_ThenChangesTotal()
    {
        var result = _usage.SetTotal(2);

        result.Should().BeSuccess();
        _usage.Total.Should().Be(2);
    }
}