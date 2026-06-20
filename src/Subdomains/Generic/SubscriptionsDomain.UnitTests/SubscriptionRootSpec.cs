using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace SubscriptionsDomain.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionRootSpec
{
    private readonly Mock<IBillingStateInterpreter> _interpreter;
    private readonly SubscriptionRoot _subscription;

    public SubscriptionRootSpec()
    {
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(x => x.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var recorder = new Mock<IRecorder>();
        var quotas = ProviderQuotas.Create(
            new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
            {
                {
                    BillingSubscriptionTier.Standard,
                    ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription").Value).Value
                }
            }).Value;
        _interpreter = new Mock<IBillingStateInterpreter>();
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = ManagementOptions.RequiresManaged,
                QuotaManagement = ManagementOptions.RequiresManaged,
                ManagedQuotas = quotas
            });
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("aprovidername");
        _interpreter.Setup(bsi => bsi.GetBuyerReference(It.IsAny<BillingProvider>()))
            .Returns("abuyerreference");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(It.IsAny<BillingProvider>()))
            .Returns("asubscriptionreference".ToOptional());
        _interpreter.Setup(bsi => bsi.SetInitialProviderState(It.IsAny<BillingProvider>()))
            .Returns((BillingProvider provider) => provider);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Empty);

        _subscription = SubscriptionRoot.Create(recorder.Object, identifierFactory.Object, "anowningentityid".ToId(),
            "abuyerid".ToId(), _interpreter.Object).Value;
    }

    [Fact]
    public void WhenCreate_ThenAssigned()
    {
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.OwningEntityId.Should().Be("anowningentityid".ToId());
        Enumerable.Last(_subscription.Events).Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenEnsureInvariantsAndBuyerIdIsEmpty_ThenReturnsErrors()
    {
#if TESTINGONLY
        _subscription.TestingOnly_SetDetails(Identifier.Empty(), _subscription.OwningEntityId);
#endif

        var result = _subscription.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoBuyer);
    }

    [Fact]
    public void WhenEnsureInvariantsAndOwningEntityIdIsEmpty_ThenReturnsErrors()
    {
#if TESTINGONLY
        _subscription.TestingOnly_SetDetails(_subscription.BuyerId, Identifier.Empty());
#endif

        var result = _subscription.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoOwningEntity);
    }

    [Fact]
    public async Task WhenSetProviderByAnotherUser_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = await _subscription.SetProviderAsync(provider, "anotheruserid".ToId(),
            _interpreter.Object);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.SubscriptionRoot_NotBuyer);
    }

    [Fact]
    public async Task WhenSetProviderAndAlreadyInitialized_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;
        await _subscription.ChangeProviderAsync(provider, CallerConstants.MaintenanceAccountUserId.ToId(),
            _interpreter.Object, (_, _, _) => Task.FromResult(Result.Ok));

        var result = await _subscription.SetProviderAsync(provider, "abuyerid".ToId(),
            _interpreter.Object);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_SameProvider);
    }

    [Fact]
    public async Task WhenSetProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result = await _subscription.SetProviderAsync(provider, "abuyerid".ToId(),
            _interpreter.Object);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenSetProvider_ThenSets()
    {
        var metadata = new SubscriptionMetadata { { "aname", "avalue" } };
        var provider = BillingProvider.Create("aprovidername", metadata).Value;

        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(),
            _interpreter.Object);

        _subscription.Provider.Value.Name.Should().Be("aprovidername");
        _subscription.Provider.Value.State.Should().BeEquivalentTo(metadata);
        Enumerable.Last(_subscription.Events).Should().BeOfType<ProviderChanged>();
        _interpreter.Verify(bsi => bsi.SetInitialProviderState(provider));
    }

    [Fact]
    public async Task WhenChangeProviderByAnyUser_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;

        var result = await _subscription.ChangeProviderAsync(provider, "auserid".ToId(),
            _interpreter.Object, (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation, Resources.SubscriptionRoot_ChangeProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenChangeProviderAndAlreadyInitialized_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;
        await _subscription.ChangeProviderAsync(provider, CallerConstants.MaintenanceAccountUserId.ToId(),
            _interpreter.Object, (_, _, _) => Task.FromResult(Result.Ok));

        var result = await _subscription.ChangeProviderAsync(provider, CallerConstants.MaintenanceAccountUserId.ToId(),
            _interpreter.Object, (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_SameProvider);
    }

    [Fact]
    public async Task WhenChangeProviderAndNotSameAsInstalledProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata { { "aname", "avalue" } })
            .Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result = await _subscription.ChangeProviderAsync(provider, CallerConstants.MaintenanceAccountUserId.ToId(),
            _interpreter.Object, (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenChangeBillingProvider_ThenChanged()
    {
        var metadata = new SubscriptionMetadata
        {
            { "aname", "avalue" }
        };
        var provider = BillingProvider.Create("aprovidername", metadata).Value;

        await _subscription.ChangeProviderAsync(provider, CallerConstants.MaintenanceAccountUserId.ToId(),
            _interpreter.Object, (_, _, _) => Task.FromResult(Result.Ok));

        _subscription.Provider.Value.Name.Should().Be("aprovidername");
        _subscription.Provider.Value.State.Should().BeEquivalentTo(metadata);
        Enumerable.Last(_subscription.Events).Should().BeOfType<ProviderChanged>();
        _interpreter.Verify(bsi => bsi.SetInitialProviderState(provider), Times.Never);
    }

    [Fact]
    public async Task WhenViewSubscriptionAsyncByBuyerButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ViewSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            (_, _) => Task.FromResult(Permission.Denied_Rule("areason")));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ViewSubscription_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenViewSubscriptionAsyncByBuyer_ThenReturnsSubscription()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        var providerSubscription = ProviderSubscription.Empty;
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(providerSubscription);

        var result = await _subscription.ViewSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            (_, _) => Task.FromResult(Permission.Allowed));

        result.Should().BeSuccess();
        result.Value.Should().Be(providerSubscription);
        _interpreter.Verify(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()));
    }

    [Fact]
    public async Task WhenViewSubscriptionAsyncByServiceAccount_ThenReturnsSubscription()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        var providerSubscription = ProviderSubscription.Empty;
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(providerSubscription);

        var result = await _subscription.ViewSubscriptionAsync(_interpreter.Object,
            CallerConstants.MaintenanceAccountUserId.ToId(),
            (_, _) => Task.FromResult(Permission.Allowed));

        result.Should().BeSuccess();
        result.Value.Should().Be(providerSubscription);
        _interpreter.Verify(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()));
    }

    [Fact]
    public async Task WhenChangePlanAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "amodifierid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenChangePlanAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "amodifierid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByBuyerButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "abuyerid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_ChangePlan_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByBuyerButNoPaymentMethod_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Empty);

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "abuyerid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.FeatureViolation, Resources.SubscriptionRoot_ChangePlan_InvalidPaymentMethod);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByBuyerWithPaymentMethodAndNoSubscription_ThenChangesPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        _interpreter.Setup(bsi => bsi.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(provider))
            .Returns(Optional<string>.None);

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "abuyerid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.IsConverted.Should().BeFalse();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().BeNone();
        _subscription.Events.Count.Should().Be(3);
        _subscription.Events[1].Should().BeOfType<ProviderChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionPlanChanged>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
        _interpreter.Verify(bsi => bsi.GetBuyerReference(provider));
        _interpreter.Verify(bsi => bsi.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByBuyerWithPaymentMethodAndSubscriptionAndAlreadyConverted_ThenChangesPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference2".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                        BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        await _subscription.InitializeSubscriptionAsync(_interpreter.Object, provider,
            "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));
        _interpreter.Setup(bsi => bsi.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "abuyerid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.IsConverted.Should().BeTrue();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Count.Should().Be(7);
        _subscription.Events[5].Should().BeOfType<SubscriptionConverted>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionPlanChanged>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
        _interpreter.Verify(bsi => bsi.GetBuyerReference(provider));
        _interpreter.Verify(bsi => bsi.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByBuyerWithPaymentMethodAndNotConverted_ThenChangesPlanAndConverts()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference2".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                        BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        _interpreter.Setup(bsi => bsi.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "abuyerid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.IsConverted.Should().BeTrue();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Count.Should().Be(4);
        _subscription.Events[2].Should().BeOfType<SubscriptionPlanChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
        _interpreter.Verify(bsi => bsi.GetBuyerReference(provider));
        _interpreter.Verify(bsi => bsi.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByWebhookWithPaymentMethod_ThenChangesPlanAndConverts()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference2".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                        BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        _interpreter.Setup(bsi => bsi.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.ChangePlanAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.IsConverted.Should().BeTrue();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events[2].Should().BeOfType<SubscriptionPlanChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
        _interpreter.Verify(bsi => bsi.GetBuyerReference(provider));
        _interpreter.Verify(bsi => bsi.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByAnotherButNoPaymentMethod_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Empty);

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "auserid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.FeatureViolation,
            Resources.SubscriptionRoot_TransferSubscription_InvalidPaymentMethod);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByAnotherButSubscriptionIsNotCanceled_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "auserid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ChangePlan_NotClaimable);
    }

    [Fact]
    public async Task WhenChangePlanAsyncByAnotherAndCanceled_ThenTransfersPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Canceled, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        _interpreter.Setup(bsi => bsi.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "auserid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("auserid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionTransferred>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
        _interpreter.Verify(bsi => bsi.GetBuyerReference(provider));
        _interpreter.Verify(bsi => bsi.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenChangePlanAsyncByAnotherAndUnsubscribed_ThenTransfersPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Unsubscribed, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        _interpreter.Setup(bsi => bsi.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.ChangePlanAsync(_interpreter.Object, "auserid".ToId(), "aplanid",
            (_, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("auserid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionTransferred>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
        _interpreter.Verify(bsi => bsi.GetBuyerReference(provider));
        _interpreter.Verify(bsi => bsi.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "acancellerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()), false);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "acancellerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()), false);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncByBuyerButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()), false);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_CancelSubscription_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncByBuyerButNotCancellable_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Canceled, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()), false);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.SubscriptionRoot_CancelSubscription_NotCancellable);
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncByOperations_ThenCanceled()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "anotheruserid".ToId(),
            Roles.Create(PlatformRoles.Operations).Value,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }), false);

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Last().Should().BeOfType<SubscriptionCanceled>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
    }

    [Fact]
    public async Task WhenCancelSubscriptionAsyncByBuyer_ThenCanceled()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));

        var result = await _subscription.CancelSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            Roles.Empty,
            (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }), false);

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Last().Should().BeOfType<SubscriptionCanceled>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "atransfererid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "atransfererid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByAnother_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "auserid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_NotBuyer);
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByBuyerButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_TransferSubscription_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByBuyerButNoPaymentMethod_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Empty);

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.FeatureViolation,
            Resources.SubscriptionRoot_TransferSubscription_InvalidPaymentMethod);
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByBuyerAndActivated_ThenTransfersPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        _interpreter.Setup(bsi => bsi.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("atransfereeid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionTransferred>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
        _interpreter.Verify(bsi => bsi.GetBuyerReference(provider));
        _interpreter.Verify(bsi => bsi.GetSubscriptionReference(provider));
    }

    [Fact]
    public async Task WhenTransferSubscriptionAsyncByBuyerAndCanceled_ThenTransfersPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Canceled, Optional<DateTime>.None, true).Value,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        _interpreter.Setup(bsi => bsi.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = await _subscription.TransferSubscriptionAsync(_interpreter.Object, "abuyerid".ToId(),
            "atransfereeid".ToId(),
            (_, _, _) => Task.FromResult(Permission.Allowed),
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        _subscription.BuyerId.Should().Be("atransfereeid".ToId());
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference2");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference2");
        _subscription.Events.Last().Should().BeOfType<SubscriptionTransferred>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
        _interpreter.Verify(bsi => bsi.GetBuyerReference(provider));
        _interpreter.Verify(bsi => bsi.GetSubscriptionReference(provider));
    }

    [Fact]
    public void WhenDeleteSubscriptionWithWrongOwningEntityId_ThenReturnsError()
    {
        var result = _subscription.DeleteSubscription("adeleterid".ToId(), "anotherentityid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_DeleteSubscription_NotOwningEntityId);
    }

    [Fact]
    public void WhenDeleteSubscription_ThenDeletes()
    {
        var result = _subscription.DeleteSubscription("adeleterid".ToId(), "anowningentityid".ToId());

        result.Should().BeSuccess();
        _subscription.Events.Last().Should().BeOfType<Deleted>();
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsyncButNotAllowed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Denied_Rule("areason")),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_UnsubscribeSubscription_FailedWithReason.Format("areason"));
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsyncButCannotBeUnsubscribed_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription
                .Create(ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false)
                    .Value).Value);

        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionRoot_CannotBeUnsubscribed);
    }

    [Fact]
    public async Task WhenUnsubscribeSubscriptionAsync_ThenUnsubscribes()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription
                .Create(ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value)
                .Value);

        var result = await _subscription.UnsubscribeSubscriptionAsync(_interpreter.Object, "unsubscriberid".ToId(),
            false, (_, _) => Task.FromResult(Permission.Allowed),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.ProviderSubscriptionReference.Should().BeNone();
        _subscription.Events.Last().Should().BeOfType<SubscriptionUnsubscribed>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncAndNoProvider_ThenReturnsError()
    {
        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "amodifierid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncAndDifferentProvider_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "amodifierid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_NoProvider);
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncByAnother_ThenReturnsError()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "amodifierid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ChangeBuyerPaymentMethodByProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncByBuyerAndNoSubscription_ThenChangesPlan()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription
                .Create(ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value)
                .Value);

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "abuyerid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Count.Should().Be(3);
        _subscription.Events[1].Should().BeOfType<ProviderChanged>();
        _subscription.Events.Last().Should().BeOfType<PaymentMethodChanged>();
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncByBuyerAndAlreadyConverted_ThenChanges()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference2".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                        BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        await _subscription.InitializeSubscriptionAsync(_interpreter.Object, provider,
            "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "abuyerid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Count.Should().Be(7);
        _subscription.Events[5].Should().BeOfType<SubscriptionConverted>();
        _subscription.Events.Last().Should().BeOfType<PaymentMethodChanged>();
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncByBuyerAndNotConverted_ThenChangesAndConverts()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference2".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                        BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object, "abuyerid".ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Count.Should().Be(4);
        _subscription.Events[2].Should().BeOfType<PaymentMethodChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerAsyncByServiceAccount_ThenChanges()
    {
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(provider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription
                .Create(ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value)
                .Value);

        var result = await _subscription.ChangePaymentMethodForBuyerAsync(_interpreter.Object,
            CallerConstants.MaintenanceAccountUserId.ToId(),
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            }));

        result.Should().BeSuccess();
        _subscription.BuyerId.Should().Be("abuyerid".ToId());
        _subscription.Events.Last().Should().BeOfType<PaymentMethodChanged>();
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerByProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result =
            _subscription.ChangePaymentMethodForBuyerByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerByProviderWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.ChangePaymentMethodForBuyerByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerByProviderByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.ChangePaymentMethodForBuyerByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ChangeBuyerPaymentMethodByProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerByProviderAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = _subscription.ChangePaymentMethodForBuyerByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerByProviderAndNoSubscription_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;

        var result = _subscription.ChangePaymentMethodForBuyerByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.Events.Count.Should().Be(3);
        _subscription.Events[1].Should().BeOfType<ProviderChanged>();
        _subscription.Events.Last().Should().BeOfType<PaymentMethodChanged>();
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerByProviderAndAlreadyConverted_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference2".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                        BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));
        await _subscription.InitializeSubscriptionAsync(_interpreter.Object, initialProvider,
            "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));
        _interpreter.Setup(bsi => bsi.GetBuyerReference(provider))
            .Returns("abuyerreference2");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(provider))
            .Returns("asubscriptionreference2".ToOptional());

        var result = _subscription.ChangePaymentMethodForBuyerByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.Events.Count.Should().Be(7);
        _subscription.Events[5].Should().BeOfType<SubscriptionConverted>();
        _subscription.Events.Last().Should().BeOfType<PaymentMethodChanged>();
    }

    [Fact]
    public async Task WhenChangePaymentMethodForBuyerByProviderAndNotConverted_ThenChangesAndConverts()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bp => bp.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference2".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                        BillingPaymentMethodStatus.Valid,
                        Optional<DateOnly>.None, Optional<string>.None)
                    .Value));

        var result = _subscription.ChangePaymentMethodForBuyerByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.Events.Count.Should().Be(4);
        _subscription.Events[2].Should().BeOfType<PaymentMethodChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
    }

    [Fact]
    public async Task WhenCancelSubscriptionByProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result =
            _subscription.CancelSubscriptionByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenCancelSubscriptionByProviderWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.CancelSubscriptionByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenCancelSubscriptionByProviderByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.CancelSubscriptionByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_CancelSubscriptionByProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenCancelSubscriptionByProviderAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = _subscription.CancelSubscriptionByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenCancelSubscriptionByProvider_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value,
                ProviderPlanPeriod.Empty, ProviderPaymentMethod.Empty));

        var result = _subscription.CancelSubscriptionByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.Events.Last().Should().BeOfType<SubscriptionCanceled>();
    }

    [Fact]
    public async Task WhenUpdateSubscriptionByProviderAsyncAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result =
            await _subscription.UpdateSubscriptionByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider,
                (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
                (_, _, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenUpdateSubscriptionByProviderAsyncWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.UpdateSubscriptionByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider,
                (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
                (_, _, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenUpdateSubscriptionByProviderAsyncByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.UpdateSubscriptionByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider,
                (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
                (_, _, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ChangeSubscriptionPlanByProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenUpdateSubscriptionByProviderAsyncAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = await _subscription.UpdateSubscriptionByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider,
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenUpdateSubscriptionByProviderAsyncWithoutPaymentMethod_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference", ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value,
                ProviderPlanPeriod.Empty, ProviderInvoice.Default, ProviderPaymentMethod.Empty));

        var result = await _subscription.UpdateSubscriptionByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider,
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.FeatureViolation, Resources.SubscriptionRoot_ChangePlan_InvalidPaymentMethod);
    }

    [Fact]
    public async Task WhenUpdateSubscriptionByProviderAsync_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference", ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value,
                ProviderPlanPeriod.Empty, ProviderInvoice.Default, ProviderPaymentMethod.Create(
                    BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid, Optional<DateOnly>.None,
                    Optional<string>.None).Value));
        var onChangeState = new SubscriptionMetadata
        {
            { "aname3", "avalue3" }
        };
        var changedState = BillingProvider.Create("aprovidername", onChangeState).Value;

        var result = await _subscription.UpdateSubscriptionByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider,
            (_, _) => Task.FromResult<Result<SubscriptionMetadata, Error>>(onChangeState),
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(changedState);
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference");
        _subscription.Events.Count.Should().Be(4);
        _subscription.Events[2].Should().BeOfType<SubscriptionPlanChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
        _interpreter.Verify(bsi => bsi.GetSubscriptionDetails(provider));
        _interpreter.Verify(bsi => bsi.GetBuyerReference(changedState));
        _interpreter.Verify(bsi => bsi.GetSubscriptionReference(changedState));
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanByProviderAsyncAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(sp => sp.ProviderName)
            .Returns("anotherprovidername");

        var result =
            await _subscription.ChangeSubscriptionPlanByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider,
                (_, _, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanByProviderAsyncWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.ChangeSubscriptionPlanByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider,
                (_, _, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanByProviderAsyncByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.ChangeSubscriptionPlanByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider,
                (_, _, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ChangeSubscriptionPlanByProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanByProviderAsyncAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = await _subscription.ChangeSubscriptionPlanByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider,
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanByProviderAsyncAndAlreadyConverted_ThenChangesPlan()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(p => p.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference", ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value,
                ProviderPlanPeriod.Empty, ProviderInvoice.Default,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None, Optional<string>.None).Value));
        await _subscription.InitializeSubscriptionAsync(_interpreter.Object, initialProvider,
            "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        var result = await _subscription.ChangeSubscriptionPlanByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider,
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.IsConverted.Should().BeTrue();
        _subscription.Provider.Should().Be(provider);
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference");
        _subscription.Events.Count.Should().Be(7);
        _subscription.Events[5].Should().BeOfType<SubscriptionConverted>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionPlanChanged>();
    }

    [Fact]
    public async Task WhenChangeSubscriptionPlanByProviderAsyncAndNotConverted_ThenChangesPlanAndConverts()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(p => p.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference", ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value,
                ProviderPlanPeriod.Empty, ProviderInvoice.Default,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None, Optional<string>.None).Value));

        var result = await _subscription.ChangeSubscriptionPlanByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider,
            (_, _, _) => Task.FromResult(Result.Ok),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.IsConverted.Should().BeTrue();
        _subscription.Provider.Should().Be(provider);
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference");
        _subscription.Events.Count.Should().Be(4);
        _subscription.Events[2].Should().BeOfType<SubscriptionPlanChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
    }

    [Fact]
    public async Task WhenDeleteSubscriptionByProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result =
            _subscription.DeleteSubscriptionByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenDeleteSubscriptionByProviderWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.DeleteSubscriptionByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenDeleteSubscriptionByProviderByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.DeleteSubscriptionByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_DeleteSubscriptionByProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenDeleteSubscriptionByProviderAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = _subscription.DeleteSubscriptionByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenDeleteSubscriptionByProvider_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference", ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value,
                ProviderPlanPeriod.Empty, ProviderInvoice.Default, ProviderPaymentMethod.Empty));

        var result = _subscription.DeleteSubscriptionByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference");
        _subscription.ProviderSubscriptionReference.Should().BeNone();
        _subscription.Events.Last().Should().BeOfType<SubscriptionUnsubscribed>();
    }

    [Fact]
    public async Task WhenRestoreBuyerAfterDeletedByProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result =
            await _subscription.RestoreBuyerAfterDeletedByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider, _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenRestoreBuyerAfterDeletedByProviderAsyncWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.RestoreBuyerAfterDeletedByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider, _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenRestoreBuyerAfterDeletedByProviderAsyncByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.RestoreBuyerAfterDeletedByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider, _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata()));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_RestoreBuyerAfterDeletedByProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenRestoreBuyerAfterDeletedByProviderAsync_ThenRestoresBuyer()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;

        var result = await _subscription.RestoreBuyerAfterDeletedByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider,
            _ => Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
            {
                { "aname2", "avalue2" }
            }));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.ProviderBuyerReference.Should().Be("abuyerreference");
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference");
        _subscription.Events.Last().Should().BeOfType<BuyerRestored>();
    }

    [Fact]
    public async Task WhenChangeDetailsForBuyerByProviderAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result =
            _subscription.ChangeDetailsForBuyerByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenChangeDetailsForBuyerByProviderWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.ChangeDetailsForBuyerByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenChangeDetailsForBuyerByProviderByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            _subscription.ChangeDetailsForBuyerByProvider(_interpreter.Object, "amodifierid".ToId(), provider);

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_ChangeBuyerDetailsByProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenChangeDetailsForBuyerByProviderAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = _subscription.ChangeDetailsForBuyerByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenChangeDetailsForBuyerByProvider_ThenChanges()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;

        var result = _subscription.ChangeDetailsForBuyerByProvider(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider);

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.Events.Last().Should().BeOfType<BuyerDetailsChanged>();
    }

    [Fact]
    public async Task WhenConvertSubscriptionByProviderAsyncAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result =
            await _subscription.ConvertSubscriptionByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider, (_, _, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenConvertSubscriptionByProviderAsyncWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.ConvertSubscriptionByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider, (_, _, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenConvertSubscriptionByProviderAsyncByAnyUser_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.ConvertSubscriptionByProviderAsync(_interpreter.Object, "amodifierid".ToId(),
                provider, (_, _, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation,
            Resources.SubscriptionRoot_AddSubscriptionByProvider_NotAuthorized);
    }

    [Fact]
    public async Task WhenConvertSubscriptionByProviderAsyncAndSameState_ThenDoesNotChange()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result = await _subscription.ConvertSubscriptionByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider, (_, _, _) => Task.FromResult(Result.Ok),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenConvertSubscriptionByProviderAsyncForSelfManagedTrial_ThenConvertsAndDispatchesFirstEvent()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = ManagementOptions.SelfManaged
            });
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference",
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None, Optional<string>.None).Value).Value);

        var wasDispatched = false;
        var wasPrepared = false;
        var result = await _subscription.ConvertSubscriptionByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider, (_, _, _) =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            }, (_, _, _) =>
            {
                wasPrepared = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeFalse();
        wasPrepared.Should().BeFalse();
        _subscription.Provider.Should().Be(provider);
        _subscription.ManagedTrial.Should().BeNone();
        _subscription.Events[2].Should().BeOfType<PaymentMethodChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
    }

    [Fact]
    public async Task WhenConvertSubscriptionByProviderAsyncForManagedTrial_ThenConvertsAndDispatchesFirstEvent()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Converted,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]);
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = ManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule.Value
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference",
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None, Optional<string>.None).Value).Value);

        var wasDispatched = false;
        var wasPrepared = false;
        TrialScheduledEvent? dispatchedEvent = null;
        var result = await _subscription.ConvertSubscriptionByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider, (_, @event, _) =>
            {
                wasDispatched = true;
                dispatchedEvent = @event;
                return Task.FromResult(Result.Ok);
            }, (_, _, _) =>
            {
                wasPrepared = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeTrue();
        dispatchedEvent.Should().Be(event1);
        wasPrepared.Should().BeFalse();
        _subscription.Provider.Should().Be(provider);
        _subscription.ManagedTrial.Value.Status.Should().Be(TrialStatus.Converted);
        _subscription.ManagedTrial.Value.ConvertedAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
        _subscription.Events[2].Should().BeOfType<PaymentMethodChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
    }

    [Fact]
    public async Task WhenConvertSubscriptionByProviderAsyncAndHasNoTrial_ThenConverts()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference",
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None, Optional<string>.None).Value).Value);

        var result = await _subscription.ConvertSubscriptionByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider, (_, _, _) => Task.FromResult(Result.Ok),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(provider);
        _subscription.Events[2].Should().BeOfType<PaymentMethodChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
    }

    [Fact]
    public async Task WhenConvertSubscriptionByProviderAsyncAndHasQuotas_ThenConverts()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var standardQuotas = ProviderTierQuotas.Create(BillingSubscriptionTier.Standard,
            ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription").Value).Value
        ).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedQuotas(standardQuotas);
#endif
        var provider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(initialProvider))
            .Returns(ProviderSubscription.Create(
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value,
                ProviderPlan.Create("aplanid1", BillingSubscriptionTier.Enterprise).Value, ProviderPlanPeriod.Empty,
                ProviderPaymentMethod.Empty).Value);
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(provider))
            .Returns(ProviderSubscription.Create("asubscriptionreference",
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value,
                ProviderPlan.Create("aplanid2", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None, Optional<string>.None).Value).Value);

        var wasPrepared = false;
        BillingSubscriptionTier? fromTier = null;
        BillingSubscriptionTier? toTier = null;
        var result = await _subscription.ConvertSubscriptionByProviderAsync(_interpreter.Object,
            CallerConstants.ExternalWebhookAccountUserId.ToId(), provider, (_, _, _) => Task.FromResult(Result.Ok),
            (_, from, to) =>
            {
                wasPrepared = true;
                fromTier = from;
                toTier = to;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasPrepared.Should().BeTrue();
        fromTier.Should().Be(BillingSubscriptionTier.Enterprise);
        toTier.Should().Be(BillingSubscriptionTier.Standard);
        _subscription.Provider.Should().Be(provider);
        _subscription.Events[2].Should().BeOfType<PaymentMethodChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
    }

    [Fact]
    public async Task WhenInitializeSubscriptionAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result =
            await _subscription.InitializeSubscriptionAsync(_interpreter.Object, provider,
                "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenInitializeSubscriptionWithDifferentProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var provider = BillingProvider.Create("anotherprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;

        var result =
            await _subscription.InitializeSubscriptionAsync(_interpreter.Object, provider,
                "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_ProviderMismatch);
    }

    [Fact]
    public async Task WhenInitializeSubscriptionHasNoPaymentMethodForSelfManagedTrial_ThenDoesNothing()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = ManagementOptions.SelfManaged
            });
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var result =
            await _subscription.InitializeSubscriptionAsync(_interpreter.Object, initialProvider,
                "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference".ToOptional());
        _subscription.ManagedTrial.Should().BeNone();
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenInitializeSubscriptionAndHasNoPaymentMethodForManagedTrial_ThenStartsManagedTrial()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = ManagementOptions.RequiresManaged
            });
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var result =
            await _subscription.InitializeSubscriptionAsync(_interpreter.Object, initialProvider,
                "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference".ToOptional());
        _subscription.ManagedTrial.Value.StartedAt.Should().BeNear(DateTime.UtcNow.ToNearestHour());
        _subscription.ManagedTrial.Value.DurationDays.Should().Be(7);
        _subscription.Events.Last().Should().BeOfType<ManagedTrialStarted>();
    }

    [Fact]
    public async Task
        WhenInitializeSubscriptionAndHasSubscriptionWithPaymentMethodWithPlanForSelfManagedTrial_ThenSubscriptionAdded()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference",
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None, Optional<string>.None).Value).Value);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = ManagementOptions.SelfManaged
            });
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.InitializeSubscriptionAsync(_interpreter.Object, initialProvider,
            "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference".ToOptional());
        _subscription.ManagedTrial.Value.ConvertedAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
        _subscription.ManagedTrial.Value.Status.Should().Be(TrialStatus.Converted);
        _subscription.Events.Count.Should().Be(4);
        _subscription.Events[2].Should().BeOfType<PaymentMethodChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
    }

    [Fact]
    public async Task
        WhenInitializeSubscriptionAndHasSubscriptionWithPaymentMethodWithPlanForManagedTrial_ThenSubscriptionAddedAndStartsManagedTrial()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference",
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None, Optional<string>.None).Value).Value);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = ManagementOptions.RequiresManaged
            });
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.InitializeSubscriptionAsync(_interpreter.Object, initialProvider,
            "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference".ToOptional());
        _subscription.ManagedTrial.Value.ConvertedAt.Should().BeNear(DateTime.UtcNow.ToNearestMinute());
        _subscription.ManagedTrial.Value.Status.Should().Be(TrialStatus.Converted);
        _subscription.Events.Count.Should().Be(5);
        _subscription.Events[2].Should().BeOfType<ManagedTrialStarted>();
        _subscription.Events[3].Should().BeOfType<PaymentMethodChanged>();
        _subscription.Events.Last().Should().BeOfType<SubscriptionConverted>();
    }

    [Fact]
    public async Task
        WhenInitializeSubscriptionForSelfManagedQuota_ThenDoesNothing()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                QuotaManagement = ManagementOptions.SelfManaged
            });
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.InitializeSubscriptionAsync(_interpreter.Object, initialProvider,
            "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference".ToOptional());
        _subscription.ManagedQuotas.Should().BeNone();
        _subscription.Events.Count.Should().Be(2);
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task
        WhenInitializeSubscriptionForManagedQuotaOnUnsubscribed_ThenSavesQuotas()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                QuotaManagement = ManagementOptions.RequiresManaged,
                ManagedQuotas = null
            });
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.InitializeSubscriptionAsync(_interpreter.Object, initialProvider,
            "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference".ToOptional());
        _subscription.ManagedQuotas.Value.Tier.Should().Be(BillingSubscriptionTier.Unsubscribed);
        _subscription.ManagedQuotas.Value.Quotas.Should().BeNone();
        _subscription.Events.Count.Should().Be(3);
        _subscription.Events.Last().Should().BeOfType<ManagedQuotasStarted>();
    }

    [Fact]
    public async Task
        WhenInitializeSubscriptionForManagedQuotaOnStandard_ThenSavesQuotas()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _interpreter.Setup(bsi => bsi.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderPaymentMethod.Empty).Value);

        var standardQuotas = ProviderTierQuotas.Create(BillingSubscriptionTier.Standard,
            ProviderPlanQuotas.Create("aquotaid", ProviderPlanQuota.Create("adescription").Value).Value).Value;
        var quotas = ProviderQuotas.Create(new Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>
        {
            { standardQuotas.Tier, standardQuotas.Quotas }
        }).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                QuotaManagement = ManagementOptions.RequiresManaged,
                ManagedQuotas = quotas
            });
        await _subscription.SetProviderAsync(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var result = await _subscription.InitializeSubscriptionAsync(_interpreter.Object, initialProvider,
            "auserid".ToId(), (_, _, _) => Task.FromResult(Result.Ok), (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        _subscription.Provider.Should().Be(initialProvider);
        _subscription.ProviderSubscriptionReference.Should().Be("asubscriptionreference".ToOptional());
        _subscription.ManagedQuotas.Should().Be(standardQuotas);
        _subscription.Events.Count.Should().Be(3);
        _subscription.Events.Last().Should().BeOfType<ManagedQuotasStarted>();
    }
}