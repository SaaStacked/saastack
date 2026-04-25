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
using FluentAssertions;
using Moq;
using SubscriptionsApplication.Persistence;
using SubscriptionsDomain;
using UnitTesting.Common;
using Xunit;
using PersonName = Application.Resources.Shared.PersonName;

namespace SubscriptionsApplication.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionsApplicationTrialEventsSpec
{
    private readonly SubscriptionsApplication _application;
    private readonly Mock<IBillingProvider> _billingProvider;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ISubscriptionRepository> _repository;
    private readonly Mock<ISubscriptionTrialEventMessageQueueRepository> _trialEventMessageRepository;

    public SubscriptionsApplicationTrialEventsSpec()
    {
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(x => x.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _caller = new Mock<ICallerContext>();
        var userProfilesService = new Mock<IUserProfilesService>();
        userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
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
                    Classification = UserProfileEmailAddressClassification.Company
                }
            });
        _billingProvider = new Mock<IBillingProvider>();
        _billingProvider.Setup(bp => bp.StateInterpreter.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged
            });
        _billingProvider.Setup(bp => bp.ProviderName)
            .Returns("aprovidername");
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
        _trialEventMessageRepository = new Mock<ISubscriptionTrialEventMessageQueueRepository>();
        _trialEventMessageRepository.Setup(ter => ter.MaxMessageDelay)
            .Returns(TimeSpan.FromDays(1));
        _repository = new Mock<ISubscriptionRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionRoot root, CancellationToken _) => root);
        _repository.Setup(rep =>
                rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionRoot root, bool _, CancellationToken _) => root);

        _application = new SubscriptionsApplication(_recorder.Object, _identifierFactory.Object,
            userProfilesService.Object, _billingProvider.Object, owningEntityService.Object,
            _trialEventMessageRepository.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenDeliverSubscriptionTrialEventAsyncAndMessageNotIncludeOwningEntityId_ThenReturnsError()
    {
        var messageAsJson = new SubscriptionTrialEventMessage
        {
            OwningEntityId = null,
            ProviderName = "aprovidername"
        }.ToJson()!;

        var result =
            await _application.DeliverSubscriptionTrialEventAsync(_caller.Object, messageAsJson,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionsApplication_TrialEvent_MissingOwningEntityId);
    }

    [Fact]
    public async Task WhenDeliverSubscriptionTrialEventAsyncAndMessageNotIncludeProviderName_ThenReturnsError()
    {
        var messageAsJson = new SubscriptionTrialEventMessage
        {
            OwningEntityId = "anowningentityid",
            ProviderName = null
        }.ToJson()!;

        var result =
            await _application.DeliverSubscriptionTrialEventAsync(_caller.Object, messageAsJson,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionsApplication_TrialEvent_MissingProviderName);
    }

    [Fact]
    public async Task WhenDeliverSubscriptionTrialEventAsyncAndMessageNotEventNorSignal_ThenReturnsError()
    {
        var messageAsJson = new SubscriptionTrialEventMessage
        {
            OwningEntityId = "anowningentityid",
            ProviderName = "aprovidername",
            Event = null,
            Signal = null
        }.ToJson()!;

        var result =
            await _application.DeliverSubscriptionTrialEventAsync(_caller.Object, messageAsJson,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.SubscriptionsApplication_TrialEvent_MissingEventAndSignal);
    }

    [Fact]
    public async Task WhenDeliverSubscriptionTrialEventAsyncAndSubscriptionNotFound_ThenDoesNothing()
    {
        var messageAsJson = new SubscriptionTrialEventMessage
        {
            OwningEntityId = "anowningentityid",
            ProviderName = "aprovidername",
            Event = new QueuedTrialEvent
            {
                Action = "anaction",
                Track = "anapplieswhen",
                EventId = "aneventid",
                DelayInDays = 1,
                Metadata = new Dictionary<string, string>()
            }
        }.ToJson()!;
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<SubscriptionRoot>.None);

        var result =
            await _application.DeliverSubscriptionTrialEventAsync(_caller.Object, messageAsJson,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task WhenDeliverSubscriptionTrialEventAsyncForExpirySignalAndNotExpired_ThenDispatchesNextSignal()
    {
        var messageAsJson = new SubscriptionTrialEventMessage
        {
            OwningEntityId = "anowningentityid",
            ProviderName = "aprovidername",
            Signal = new QueuedTrialSignal
            {
                SignalId = "asignalid"
            }
        }.ToJson()!;
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var result =
            await _application.DeliverSubscriptionTrialEventAsync(_caller.Object, messageAsJson,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<SubscriptionRoot>(), It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.HandleTrialScheduledEventAsync(It.IsAny<ICallerContext>(),
            It.IsAny<SubscriptionBuyer>(), It.IsAny<TrialScheduledEvent>(),
            It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()), Times.Never);
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
            It.Is<SubscriptionTrialEventMessage>(msg =>
                msg.ProviderName == "aprovidername"
                && msg.OwningEntityId == "anowningentityid"
                && msg.Event.NotExists()
                && msg.Signal.Exists()
                && msg.Signal!.SignalId.HasValue()
            ), It.Is<TimeSpan>(ts =>
                ts > TimeSpan.Zero
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task
        WhenDeliverSubscriptionTrialEventAsyncForExpirySignalAndExpirable_ThenExpiresTrialAndCancelsSubscription()
    {
        var messageAsJson = new SubscriptionTrialEventMessage
        {
            OwningEntityId = "anowningentityid",
            ProviderName = "aprovidername",
            Signal = new QueuedTrialSignal
            {
                SignalId = "asignalid"
            }
        }.ToJson()!;
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var trial = TrialTimeline.Create(DateTime.UtcNow.SubtractDays(10), 1).Value;
#if TESTINGONLY
        subscription.TestingOnly_SetManagedTrial(trial);
#endif
        var canceledMetadata =
            new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "acanceledvalue" } });
        _billingProvider.Setup(bp => bp.GatewayService.CancelSubscriptionAsync(It.IsAny<ICallerContext>(),
                It.IsAny<CancelSubscriptionOptions>(), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(canceledMetadata);

        var result =
            await _application.DeliverSubscriptionTrialEventAsync(_caller.Object, messageAsJson,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.ManagedTrial.Value.Status == TrialStatus.Expired
        ), It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.CancelSubscriptionAsync(_caller.Object,
            It.Is<CancelSubscriptionOptions>(options =>
                options.CancelWhen == CancelSubscriptionSchedule.Immediately),
            It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.HandleTrialScheduledEventAsync(It.IsAny<ICallerContext>(),
            It.IsAny<SubscriptionBuyer>(), It.IsAny<TrialScheduledEvent>(),
            It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()), Times.Never);
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
                It.IsAny<SubscriptionTrialEventMessage>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _billingProvider.Verify(bp => bp.GatewayService.CancelSubscriptionAsync(It.IsAny<ICallerContext>(),
            It.Is<CancelSubscriptionOptions>(opts =>
                opts.CancelWhen == CancelSubscriptionSchedule.Immediately
            ), It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeliverSubscriptionTrialEventAsyncAndScheduleHasNoMoreMessages_ThenDeliversEventToProvider()
    {
        var messageAsJson = new SubscriptionTrialEventMessage
        {
            OwningEntityId = "anowningentityid",
            ProviderName = "aprovidername",
            Event = new QueuedTrialEvent
            {
                Action = nameof(TrialScheduledEventAction.Notification),
                Track = "anapplieswhen",
                DelayInDays = 1,
                EventId = "aneventid",
                Metadata = new Dictionary<string, string>()
            }
        }.ToJson()!;
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        subscription.TestingOnly_SetManagedTrial(trial);
#endif
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var event1 = TrialScheduledEvent.Create(1, "aneventid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]);
        _billingProvider.Setup(bp => bp.StateInterpreter.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule.Value
            });
        _billingProvider.Setup(bp => bp.GatewayService.HandleTrialScheduledEventAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<TrialScheduledEvent>(),
                It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.DeliverSubscriptionTrialEventAsync(_caller.Object, messageAsJson,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.LastScheduledTrialEventId == "aneventid"
        ), It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.HandleTrialScheduledEventAsync(_caller.Object,
            It.Is<SubscriptionBuyer>(buyer =>
                buyer.Id == "abuyerid"
            ), It.Is<TrialScheduledEvent>(msg =>
                msg.Id == "aneventid"
                && msg.DelayInDays == 1
                && msg.Action == TrialScheduledEventAction.Notification
                && msg.Metadata.Items.HasNone()
            ), subscription.Provider, It.IsAny<CancellationToken>()));
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
            It.IsAny<SubscriptionTrialEventMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task
        WhenDeliverSubscriptionTrialEventAsyncAndHasNextMessage_ThenDeliversEventToProviderAndDispatchesNextMessage()
    {
        var messageAsJson = new SubscriptionTrialEventMessage
        {
            OwningEntityId = "anowningentityid",
            ProviderName = "aprovidername",
            Event = new QueuedTrialEvent
            {
                Action = nameof(TrialScheduledEventAction.Notification),
                Track = "anapplieswhen",
                DelayInDays = 1,
                EventId = "aneventid1",
                Metadata = new Dictionary<string, string>()
            }
        }.ToJson()!;
        var subscription = SubscriptionRoot.Create(_recorder.Object, _identifierFactory.Object,
            "anowningentityid".ToId(), "abuyerid".ToId(), _billingProvider.Object.StateInterpreter).Value;
        var metadata = new SubscriptionMetadata(new Dictionary<string, string> { { "aname", "avalue" } });
        subscription.SetProvider(BillingProvider.Create("aprovidername", metadata).Value,
            "abuyerid".ToId(), _billingProvider.Object.StateInterpreter);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        subscription.TestingOnly_SetManagedTrial(trial);
#endif
        _repository.Setup(rep => rep.FindByOwningEntityIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription.ToOptional());
        var event1 = TrialScheduledEvent.Create(1, "aneventid1", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var event2 = TrialScheduledEvent.Create(1, "aneventid2", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1, event2]);
        _billingProvider.Setup(bp => bp.StateInterpreter.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule.Value
            });
        _billingProvider.Setup(bp => bp.GatewayService.HandleTrialScheduledEventAsync(It.IsAny<ICallerContext>(),
                It.IsAny<SubscriptionBuyer>(), It.IsAny<TrialScheduledEvent>(),
                It.IsAny<BillingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.DeliverSubscriptionTrialEventAsync(_caller.Object, messageAsJson,
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _repository.Verify(rep => rep.SaveAsync(It.Is<SubscriptionRoot>(root =>
            root.LastScheduledTrialEventId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _billingProvider.Verify(bp => bp.GatewayService.HandleTrialScheduledEventAsync(_caller.Object,
            It.Is<SubscriptionBuyer>(buyer =>
                buyer.Id == "abuyerid"
            ),
            It.Is<TrialScheduledEvent>(msg =>
                msg.Id == "aneventid1"
                && msg.DelayInDays == 1
                && msg.Action == TrialScheduledEventAction.Notification
                && msg.Metadata.Items.HasNone()
            ), subscription.Provider, It.IsAny<CancellationToken>()));
        _trialEventMessageRepository.Verify(rep => rep.PushAsync(It.IsAny<ICallContext>(),
            It.Is<SubscriptionTrialEventMessage>(msg =>
                msg.ProviderName == "aprovidername"
                && msg.OwningEntityId == "anowningentityid"
                && msg.Signal.NotExists()
                && msg.Event!.EventId == "aneventid2"
                && msg.Event.Action == nameof(TrialScheduledEventAction.Notification)
                && msg.Event.Track == nameof(TrialScheduledEventTrack.Active)
                && msg.Event.Metadata.HasNone()
            ), It.Is<TimeSpan>(ts =>
                ts == TimeSpan.FromDays(1)
            ), It.IsAny<CancellationToken>()));
    }
}