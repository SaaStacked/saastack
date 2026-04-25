using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;
using Moq;
using SubscriptionsApplication.Persistence;
using SubscriptionsDomain;
using UnitTesting.Common;
using Xunit;

namespace SubscriptionsApplication.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionsApplicationProviderNotificationsSpec
{
    private readonly SubscriptionsApplication _application;
    private readonly Mock<IBillingProvider> _billingProvider;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ISubscriptionRepository> _repository;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public SubscriptionsApplicationProviderNotificationsSpec()
    {
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(x => x.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _caller = new Mock<ICallerContext>();
        _userProfilesService = new Mock<IUserProfilesService>();
        _billingProvider = new Mock<IBillingProvider>();
        _billingProvider.Setup(bp => bp.ProviderName)
            .Returns("aprovidername");
        _billingProvider.Setup(bp => bp.StateInterpreter.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.SelfManaged
            });
        _billingProvider.Setup(bp => bp.StateInterpreter.ProviderName)
            .Returns("aprovidername");
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.SetInitialProviderState(It.IsAny<BillingProvider>()))
            .Returns((BillingProvider provider) => provider);
        _billingProvider.Setup(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()))
            .Returns("abuyerreference");
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()))
            .Returns("asubscriptionreference".ToOptional());
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, ProviderPaymentMethod.Empty).Value);
        var owningEntityService = new Mock<ISubscriptionOwningEntityService>();
        owningEntityService.Setup(oes =>
                oes.GetEntityAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OwningEntity
            {
                Id = "anowningentityid",
                Type = "anowningentitytype",
                Name = "anowningentityname"
            });
        var subscriptionEventMessageRepository = new Mock<ISubscriptionTrialEventMessageQueueRepository>();
        _repository = new Mock<ISubscriptionRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionRoot root, CancellationToken _) => root);

        _application = new SubscriptionsApplication(_recorder.Object, _identifierFactory.Object,
            _userProfilesService.Object, _billingProvider.Object, owningEntityService.Object,
            subscriptionEventMessageRepository.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenNotifyNotifyBuyerPaymentMethodChangedAsyncAndNoSubscription_ThenChangesNone()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _repository.Setup(rep => rep.FindByBuyerReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result = await _application.NotifyBuyerPaymentMethodChangedAsync(_caller.Object, "aprovidername",
            metadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyBuyerPaymentMethodChangedAsync_ThenChangesPaymentMethod()
    {
        var initialMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        });
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.ExternalWebhookAccountUserId);
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", initialMetadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByBuyerReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var changedMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });

        var result = await _application.NotifyBuyerPaymentMethodChangedAsync(_caller.Object, "aprovidername",
            changedMetadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.Events.Last() is PaymentMethodChanged
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifyBuyerDeletedAsyncAndNoSubscription_ThenChangesNone()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _repository.Setup(rep => rep.FindByBuyerReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result = await _application.NotifyBuyerDeletedAsync(_caller.Object, "aprovidername",
            metadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyBuyerDeletedAsync_ThenRestoresBuyer()
    {
        var initialMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        });
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.ExternalWebhookAccountUserId);
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", initialMetadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByBuyerReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _userProfilesService.Setup(ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                UserId = "auserid",
                Id = "aprofileid",
                EmailAddress = new UserProfileEmailAddress
                {
                    Address = "auser@company.com",
                    Classification = UserProfileEmailAddressClassification.Personal
                }
            });
        _billingProvider.Setup(bp =>
                bp.GatewayService.RestoreBuyerAsync(It.IsAny<ICallerContext>(), It.IsAny<SubscriptionBuyer>(),
                    CancellationToken.None))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname2", "avalue2" }
            });
        var changedMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname3", "avalue3" }
        });

        var result = await _application.NotifyBuyerDeletedAsync(_caller.Object, "aprovidername",
            changedMetadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.Events.Last() is BuyerRestored
        ), It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.RestoreBuyerAsync(_caller.Object,
            It.Is<SubscriptionBuyer>(sub =>
                sub.Id == "abuyerid"
                && sub.Name.FirstName == "afirstname"
                && sub.Name.LastName == "alastname"
                && sub.EmailAddress == "auser@company.com"
                && sub.Subscriber.EntityId == "anowningentityid"
                && sub.Subscriber.EntityType == "anowningentitytype"
                && sub.Subscriber.EntityName == "anowningentityname"
                && sub.Address.CountryCode == CountryCodes.Default.ToString()
            ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups => ups.GetProfilePrivateAsync(_caller.Object, "abuyerid",
            CancellationToken.None));
    }

    [Fact]
    public async Task WhenNotifySubscriptionCanceledAsyncAndNoSubscription_ThenChangesNone()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _repository
            .Setup(rep => rep.FindBySubscriptionReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result = await _application.NotifySubscriptionCanceledAsync(_caller.Object, "aprovidername",
            metadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifySubscriptionCanceledAsync_ThenChangesPaymentMethod()
    {
        var initialMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        });
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.ExternalWebhookAccountUserId);
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", initialMetadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository
            .Setup(rep => rep.FindBySubscriptionReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var changedMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });

        var result = await _application.NotifySubscriptionCanceledAsync(_caller.Object, "aprovidername",
            changedMetadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.Events.Last() is SubscriptionCanceled
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifySubscriptionDetailsChangedAsyncAndNoSubscription_ThenChangesNone()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _repository
            .Setup(rep => rep.FindBySubscriptionReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result = await _application.NotifySubscriptionDetailsChangedAsync(_caller.Object, "aprovidername",
            metadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifySubscriptionDetailsChangedAsync_ThenChangesPaymentMethod()
    {
        var initialMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        });
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.ExternalWebhookAccountUserId);
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", initialMetadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository
            .Setup(rep => rep.FindBySubscriptionReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var changedMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference".ToId(), ProviderStatus.Empty,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default, ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                    BillingPaymentMethodStatus.Valid, Optional<DateOnly>.None, Optional<string>.None).Value).Value);
        _billingProvider.Setup(bp => bp.GatewayService.ReSyncSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname3", "avalue3" }
            });

        var result = await _application.NotifySubscriptionDetailsChangedAsync(_caller.Object, "aprovidername",
            changedMetadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.Events.Last() is SubscriptionPlanChanged
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifySubscriptionPlanChangedAsyncAndNoSubscription_ThenChangesNone()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _repository
            .Setup(rep => rep.FindBySubscriptionReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result = await _application.NotifySubscriptionPlanChangedAsync(_caller.Object, "aprovidername",
            metadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifySubscriptionPlanChangedAsync_ThenChangesPaymentMethod()
    {
        var initialMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        });
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.ExternalWebhookAccountUserId);
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", initialMetadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository
            .Setup(rep => rep.FindBySubscriptionReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var changedMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });

        var result = await _application.NotifySubscriptionPlanChangedAsync(_caller.Object, "aprovidername",
            changedMetadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.Events.Last() is SubscriptionPlanChanged
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifySubscriptionDeletedAsyncAndNoSubscription_ThenChangesNone()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _repository
            .Setup(rep => rep.FindBySubscriptionReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result = await _application.NotifySubscriptionDeletedAsync(_caller.Object, "aprovidername",
            metadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifySubscriptionDeletedAsync_ThenChangesPaymentMethod()
    {
        var initialMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        });
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.ExternalWebhookAccountUserId);
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", initialMetadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository
            .Setup(rep => rep.FindBySubscriptionReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var changedMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });

        var result = await _application.NotifySubscriptionDeletedAsync(_caller.Object, "aprovidername",
            changedMetadata, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.Events.Last() is SubscriptionUnsubscribed
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifyBuyerDetailsChangedAsyncAndNoCustomer_ThenChangesNone()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _repository.Setup(rep => rep.FindByBuyerReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result =
            await _application.NotifyBuyerDetailsChangedAsync(_caller.Object, "aprovidername", metadata,
                CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyBuyerDetailsChangedAsync_ThenChangesBuyerDetails()
    {
        var initialMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        });
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.ExternalWebhookAccountUserId);
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", initialMetadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByBuyerReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var changedMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });

        var result = await _application.NotifyBuyerDetailsChangedAsync(_caller.Object, "aprovidername", changedMetadata,
            CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.Events.Last() is BuyerDetailsChanged
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifyBuyerSubscriptionAddedAsyncAndNoCustomer_ThenChangesNone()
    {
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        _repository.Setup(rep => rep.FindByBuyerReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result =
            await _application.NotifyBuyerSubscriptionAddedWithPaymentMethodAsync(_caller.Object, "aprovidername",
                metadata,
                CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenNotifyBuyerSubscriptionAddedAsync_ThenAddsSubscription()
    {
        var initialMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        });
        _caller.Setup(cc => cc.CallerId)
            .Returns(CallerConstants.ExternalWebhookAccountUserId);
        var subscription = SubscriptionRoot
            .Create(_recorder.Object, _identifierFactory.Object, "anowningentityid".ToId(), "abuyerid".ToId(),
                _billingProvider.Object.StateInterpreter).Value;
        subscription.SetProvider(BillingProvider.Create("aprovidername", initialMetadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByBuyerReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var changedMetadata = new SubscriptionMetadata(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create("asubscriptionreference".ToId(),
                ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, false).Value,
                ProviderPlan.Create("aplanid", BillingSubscriptionTier.Standard).Value, ProviderPlanPeriod.Empty,
                ProviderInvoice.Default,
                ProviderPaymentMethod.Create(BillingPaymentMethodType.Card, BillingPaymentMethodStatus.Valid,
                    Optional<DateOnly>.None, Optional<string>.None).Value).Value);

        var result = await _application.NotifyBuyerSubscriptionAddedWithPaymentMethodAsync(_caller.Object,
            "aprovidername", changedMetadata,
            CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.Events.Count == 4
            && root.Events[2] is PaymentMethodChanged
            && root.Events.Last() is SubscriptionConverted
        ), It.IsAny<CancellationToken>()));
    }
}