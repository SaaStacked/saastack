using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Subscriptions;
using Moq;
using OrganizationsDomain;
using SubscriptionsApplication.Persistence;
using SubscriptionsDomain;
using UnitTesting.Common;
using UserProfilesDomain;
using Xunit;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;
using OrganizationsDomainEvents = OrganizationsDomain.Events;
using PersonName = Application.Resources.Shared.PersonName;
using UserProfileEvents = Domain.Events.Shared.UserProfiles;

namespace SubscriptionsApplication.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionsApplicationDomainEventHandlersSpec
{
    private readonly SubscriptionsApplication _application;
    private readonly Mock<IBillingProvider> _billingProvider;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ISubscriptionRepository> _repository;
    private readonly Mock<ISubscriptionTrialEventMessageQueueRepository> _trialEventMessageRepository;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public SubscriptionsApplicationDomainEventHandlersSpec()
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
        var providerCapabilities = new BillingProviderCapabilities
        {
            TrialManagement = TrialManagementOptions.SelfManaged
        };
        _billingProvider.Setup(bp => bp.Capabilities)
            .Returns(providerCapabilities);
        _billingProvider.Setup(bp => bp.StateInterpreter.Capabilities)
            .Returns(providerCapabilities);
        _billingProvider.Setup(bp => bp.StateInterpreter.ProviderName)
            .Returns("aprovidername");
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.SetInitialProviderState(It.IsAny<BillingProvider>()))
            .Returns((BillingProvider provider) => provider);
        _billingProvider.Setup(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()))
            .Returns("abuyerreference");
        _billingProvider.Setup(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()))
            .Returns("asubscriptionreference".ToOptional());
        var owningEntityService = new Mock<ISubscriptionOwningEntityService>();
        owningEntityService.Setup(oes =>
                oes.GetEntityAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OwningEntity
            {
                Id = "anowningentityid",
                Type = "anowningentitytype",
                Name = "anowningentityname"
            });
        _trialEventMessageRepository = new Mock<ISubscriptionTrialEventMessageQueueRepository>();
        _trialEventMessageRepository.Setup(ter => ter.MaxMessageDelay)
            .Returns(TimeSpan.FromDays(2));
        _repository = new Mock<ISubscriptionRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionRoot root, CancellationToken _) => root);

        _application = new SubscriptionsApplication(_recorder.Object, _identifierFactory.Object,
            _userProfilesService.Object, _billingProvider.Object, owningEntityService.Object,
            _trialEventMessageRepository.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenHandleOrganizationCreatedAsyncAndProfileNotExists_ThenCreatesPartialSubscription()
    {
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());
        _billingProvider.Setup(bp => bp.GatewayService.SubscribeAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<SubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            });

        var domainEvent = OrganizationsDomainEvents.Created("anowningentityid".ToId(), OrganizationOwnership.Personal,
            "abuyerid".ToId(), Optional<EmailAddress>.None, DisplayName.Create("aname").Value,
            DatacenterLocations.Local);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "abuyerid".ToId()
            && root.OwningEntityId == "anowningentityid".ToId()
            && root.Provider.HasValue == false
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ps =>
            ps.GetProfilePrivateAsync(_caller.Object, "abuyerid".ToId(), CancellationToken.None));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()), Times.Never);
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()),
            Times.Never);
    }

    [Fact]
    public async Task
        WhenHandleOrganizationCreatedAsyncAndProfileExistsForSelfManagedTrial_ThenCreatesCompletedSubscription()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), stateInterpreter.Object).Value;
        _repository.Setup(r =>
                r.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                EmailAddress = new UserProfileEmailAddress
                {
                    Address = "anemailaddress",
                    Classification = UserProfileEmailAddressClassification.Personal
                },
                PhoneNumber = "aphonenumber",
                Classification = UserProfileClassification.Person,
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "abuyerid",
                Id = "aprofileid"
            });
        _billingProvider.Setup(bp => bp.GatewayService.SubscribeAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<SubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            });

        var domainEvent = OrganizationsDomainEvents.Created("anowningentityid".ToId(), OrganizationOwnership.Personal,
            "abuyerid".ToId(), Optional<EmailAddress>.None, DisplayName.Create("aname").Value,
            DatacenterLocations.Local);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "abuyerid".ToId()
            && root.OwningEntityId == "anowningentityid".ToId()
            && root.Provider.Value.Name == "aprovidername"
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ps =>
            ps.GetProfilePrivateAsync(_caller.Object, "abuyerid".ToId(), CancellationToken.None));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()));
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
                It.IsAny<SubscriptionTrialEventMessage>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
                It.IsAny<SubscriptionTrialEventMessage>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task
        WhenHandleOrganizationCreatedAsyncAndProfileExistsForManagedTrial_ThenCreatesCompletedSubscription()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), stateInterpreter.Object).Value;
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        subscription.TestingOnly_SetManagedTrial(trial);
#endif
        _repository.Setup(r =>
                r.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                EmailAddress = new UserProfileEmailAddress
                {
                    Address = "anemailaddress",
                    Classification = UserProfileEmailAddressClassification.Personal
                },
                PhoneNumber = "aphonenumber",
                Classification = UserProfileClassification.Person,
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "abuyerid",
                Id = "aprofileid"
            });
        _billingProvider.Setup(bp => bp.GatewayService.SubscribeAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<SubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            });
        var event1 = TrialScheduledEvent.Create(9, "anid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]);
        _billingProvider.Setup(bp => bp.StateInterpreter.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule.Value
            });
        var domainEvent = OrganizationsDomainEvents.Created("anowningentityid".ToId(), OrganizationOwnership.Personal,
            "abuyerid".ToId(), Optional<EmailAddress>.None, DisplayName.Create("aname").Value,
            DatacenterLocations.Local);

        var result =
            await _application.HandleOrganizationCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "abuyerid".ToId()
            && root.OwningEntityId == "anowningentityid".ToId()
            && root.Provider.Value.Name == "aprovidername"
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ps =>
            ps.GetProfilePrivateAsync(_caller.Object, "abuyerid".ToId(), CancellationToken.None));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()));
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
            It.Is<SubscriptionTrialEventMessage>(msg =>
                msg.OwningEntityId == "anowningentityid".ToId()
                && msg.ProviderName == "aprovidername"
                && msg.Signal.NotExists()
                && msg.Event!.EventId == "anid"
                && msg.Event!.Action == nameof(TrialScheduledEventAction.Notification)
                && msg.Event!.Track == nameof(TrialScheduledEventTrack.Active)
                && msg.Event!.Metadata.HasNone()
            ), It.Is<TimeSpan>(ts =>
                ts == TimeSpan.FromDays(event1.DelayInDays)
            ), It.IsAny<CancellationToken>()));
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
            It.Is<SubscriptionTrialEventMessage>(msg =>
                msg.OwningEntityId == "anowningentityid".ToId()
                && msg.ProviderName == "aprovidername"
                && msg.Event.NotExists()
                && msg.Signal!.SignalId.HasValue()
            ), It.Is<TimeSpan>(ts =>
                ts == _trialEventMessageRepository.Object.MaxMessageDelay
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleUserProfileCreatedAsyncAndSubscriptionNotExists_ThenIgnores()
    {
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        _repository.Setup(r =>
                r.FindByBuyerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                EmailAddress = new UserProfileEmailAddress
                {
                    Address = "anemailaddress",
                    Classification = UserProfileEmailAddressClassification.Personal
                },
                PhoneNumber = "aphonenumber",
                Classification = UserProfileClassification.Person,
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "auserid",
                Id = "aprofileid"
            });

        var domainEvent = new UserProfileEvents.Created("aprofileid".ToId())
        {
            DisplayName = "adisplayname",
            FirstName = "anemailaddress",
            LastName = "aphonenumber",
            Type = nameof(ProfileType.Person),
            UserId = "auserid"
        };

        var result =
            await _application.HandleUserProfileCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfilesService.Verify(
            ps => ps.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()), Times.Never);
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()),
            Times.Never);
    }

    [Fact]
    public async Task
        WhenHandleUserProfileCreatedAsyncAndPartialSubscriptionExistsForSelfManagedTrial_ThenCompletedSubscription()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "auserid".ToId(), stateInterpreter.Object).Value;
        _repository.Setup(r =>
                r.FindByBuyerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                EmailAddress = new UserProfileEmailAddress
                {
                    Address = "anemailaddress",
                    Classification = UserProfileEmailAddressClassification.Personal
                },
                PhoneNumber = "aphonenumber",
                Classification = UserProfileClassification.Person,
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "auserid",
                Id = "aprofileid"
            });
        _billingProvider.Setup(bp => bp.GatewayService.SubscribeAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<SubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            });

        var domainEvent = new UserProfileEvents.Created("aprofileid".ToId())
        {
            DisplayName = "adisplayname",
            FirstName = "anemailaddress",
            LastName = "aphonenumber",
            Type = nameof(ProfileType.Person),
            UserId = "auserid"
        };

        var result =
            await _application.HandleUserProfileCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "auserid".ToId()
            && root.OwningEntityId == "anowningentityid".ToId()
            && root.Provider.Value.Name == "aprovidername"
            && root.ManagedTrial == Optional<TrialTimeline>.None
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ps =>
            ps.GetProfilePrivateAsync(_caller.Object, "auserid".ToId(), CancellationToken.None));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()));
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
                It.IsAny<SubscriptionTrialEventMessage>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
                It.IsAny<SubscriptionTrialEventMessage>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task
        WhenHandleUserProfileCreatedAsyncAndPartialSubscriptionExistsForManagedTrial_ThenCompletedSubscription()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "auserid".ToId(), stateInterpreter.Object).Value;
        _repository.Setup(r =>
                r.FindByBuyerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                DisplayName = "adisplayname",
                EmailAddress = new UserProfileEmailAddress
                {
                    Address = "anemailaddress",
                    Classification = UserProfileEmailAddressClassification.Personal
                },
                PhoneNumber = "aphonenumber",
                Classification = UserProfileClassification.Person,
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                UserId = "auserid",
                Id = "aprofileid"
            });
        _billingProvider.Setup(bp => bp.GatewayService.SubscribeAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<SubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionMetadata
            {
                { "aname", "avalue" }
            });
        var schedule = TrialEventSchedule.Create([
            TrialScheduledEvent.Create(9, "anid", TrialScheduledEventTrack.Active,
                TrialScheduledEventAction.Notification, StringNameValues.Empty).Value
        ]);
        _billingProvider.Setup(bp => bp.StateInterpreter.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule.Value
            });
        var domainEvent = new UserProfileEvents.Created("aprofileid".ToId())
        {
            DisplayName = "adisplayname",
            FirstName = "anemailaddress",
            LastName = "aphonenumber",
            Type = nameof(ProfileType.Person),
            UserId = "auserid"
        };

        var result =
            await _application.HandleUserProfileCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.BuyerId == "auserid".ToId()
            && root.OwningEntityId == "anowningentityid".ToId()
            && root.Provider.Value.Name == "aprovidername"
            && root.ManagedTrial.Value.StartedAt.IsNear(DateTime.UtcNow.ToNearestHour())
            && root.ManagedTrial.Value.DurationDays == 7
            && !root.ManagedTrial.Value.IsConverted
        ), It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ps =>
            ps.GetProfilePrivateAsync(_caller.Object, "auserid".ToId(), CancellationToken.None));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()));
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
            It.Is<SubscriptionTrialEventMessage>(msg =>
                msg.OwningEntityId == "anowningentityid".ToId()
                && msg.ProviderName == "aprovidername"
                && msg.Signal.NotExists()
                && msg.Event!.EventId == "anid"
                && msg.Event!.Action == nameof(TrialScheduledEventAction.Notification)
                && msg.Event!.Track == nameof(TrialScheduledEventTrack.Active)
                && msg.Event!.Metadata.HasNone()
            ), It.Is<TimeSpan>(ts =>
                ts == TimeSpan.FromDays(9)
            ), It.IsAny<CancellationToken>()));
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
            It.Is<SubscriptionTrialEventMessage>(msg =>
                msg.OwningEntityId == "anowningentityid".ToId()
                && msg.ProviderName == "aprovidername"
                && msg.Event.NotExists()
                && msg.Signal!.SignalId.HasValue()
            ), It.Is<TimeSpan>(ts =>
                ts == _trialEventMessageRepository.Object.MaxMessageDelay
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleUserProfileCreatedAsyncAndCompletedSubscriptionExists_ThenIgnores()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        _billingProvider.Setup(bp =>
                bp.StateInterpreter.GetSubscriptionDetails(It.IsAny<BillingProvider>()))
            .Returns(ProviderSubscription.Create(ProviderStatus.Empty).Value);
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "auserid".ToId(), stateInterpreter.Object).Value;
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "auserid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(r =>
                r.FindByBuyerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());

        var domainEvent = new UserProfileEvents.Created("aprofileid".ToId())
        {
            DisplayName = "adisplayname",
            FirstName = "anemailaddress",
            LastName = "aphonenumber",
            Type = nameof(ProfileType.Person),
            UserId = "auserid"
        };

        var result =
            await _application.HandleUserProfileCreatedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userProfilesService.Verify(
            ps => ps.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _billingProvider.Verify(bp => bp.StateInterpreter.GetBuyerReference(It.IsAny<BillingProvider>()));
        _billingProvider.Verify(bp => bp.StateInterpreter.GetSubscriptionReference(It.IsAny<BillingProvider>()));
    }

    [Fact]
    public async Task WhenHandleOrganizationDeletedAsync_ThenReturnsOk()
    {
        var stateInterpreter = new Mock<IBillingStateInterpreter>();
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), stateInterpreter.Object).Value;
        var domainEvent = OrganizationsDomainEvents.Deleted("anowningentityid".ToId(), "adeleterid".ToId());
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());

        var result =
            await _application.HandleOrganizationDeletedAsync(_caller.Object, domainEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.IsDeleted == true
        ), It.IsAny<CancellationToken>()));
    }
}