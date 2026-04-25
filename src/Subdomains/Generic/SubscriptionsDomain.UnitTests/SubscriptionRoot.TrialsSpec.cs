using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace SubscriptionsDomain.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionRootTrialsSpec
{
    private readonly Mock<IBillingStateInterpreter> _interpreter;
    private readonly SubscriptionRoot _subscription;

    public SubscriptionRootTrialsSpec()
    {
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(x => x.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var recorder = new Mock<IRecorder>();
        _interpreter = new Mock<IBillingStateInterpreter>();
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged
            });
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("aprovidername");
        _interpreter.Setup(bsi => bsi.GetBuyerReference(It.IsAny<BillingProvider>()))
            .Returns("abuyerreference");
        _interpreter.Setup(bsi => bsi.GetSubscriptionReference(It.IsAny<BillingProvider>()))
            .Returns("asubscriptionreference".ToOptional());
        _interpreter.Setup(bsi => bsi.SetInitialProviderState(It.IsAny<BillingProvider>()))
            .Returns((BillingProvider provider) => provider);

        _subscription = SubscriptionRoot.Create(recorder.Object, identifierFactory.Object, "anowningentityid".ToId(),
            "abuyerid".ToId(), _interpreter.Object).Value;
    }

    [Fact]
    public async Task WhenDispatchManagedTrialFirstScheduledEventAsyncAndNoTrial_ThenDoesNotDispatch()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var wasDispatched = false;
        var result = await _subscription.DispatchManagedTrialFirstScheduledEventAsync(_interpreter.Object,
            (_, _, _) =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDispatchManagedTrialFirstScheduledEventAsyncAndNoTrialSchedule_ThenDoesNotDispatch()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDispatched = false;
        var result = await _subscription.DispatchManagedTrialFirstScheduledEventAsync(_interpreter.Object,
            (_, _, _) =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDispatchManagedTrialFirstScheduledEventAsyncAndNoTrialScheduleEvents_ThenDoesNotDispatch()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDispatched = false;
        var result = await _subscription.DispatchManagedTrialFirstScheduledEventAsync(_interpreter.Object,
            (_, _, _) =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDispatchManagedTrialFirstScheduledEventAsyncAndNoneForTrialState_ThenDoesNotDispatch()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Expired,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDispatched = false;
        var result = await _subscription.DispatchManagedTrialFirstScheduledEventAsync(_interpreter.Object,
            (_, _, _) =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDispatchManagedTrialFirstScheduledEventAsync_ThenDispatchesFirstEvent()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDispatched = false;
        TrialScheduledEvent? dispatchedEvent = null;
        var result = await _subscription.DispatchManagedTrialFirstScheduledEventAsync(_interpreter.Object,
            (_, @event, _) =>
            {
                wasDispatched = true;
                dispatchedEvent = @event;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeTrue();
        dispatchedEvent.Should().Be(event1);
    }

    [Fact]
    public async Task WhenDeliverManagedTrialScheduledEventAsyncAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");
        var event1 = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1,
                (_, _) => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task
        WhenDeliverManagedTrialScheduledEventAsyncAndNoLongerHasATrial_ThenDoesNotDeliverNorDispatchNextAndEndsSchedule()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

        var wasDelivered = false;
        var wasDispatched = false;
        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1, (_, _) =>
                {
                    wasDelivered = true;
                    return Task.FromResult(Result.Ok);
                },
                (_, _, _) =>
                {
                    wasDispatched = true;
                    return Task.FromResult(Result.Ok);
                });

        result.Should().BeSuccess();
        wasDelivered.Should().BeFalse();
        wasDispatched.Should().BeFalse();
        _subscription.Events.Last().Should().BeOfType<ManagedTrialEventScheduleEnded>();
        _subscription.Events.Last().As<ManagedTrialEventScheduleEnded>().Reason.Should()
            .Be(TrialScheduledEndingReason.TrialMissing);
    }

    [Fact]
    public async Task
        WhenDeliverManagedTrialScheduledEventAsyncAndNoLongerHasATrialSchedule_ThenDoesNotDeliverNorDispatchNextAndEndsSchedule()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDelivered = false;
        var wasDispatched = false;
        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1, (_, _) =>
                {
                    wasDelivered = true;
                    return Task.FromResult(Result.Ok);
                },
                (_, _, _) =>
                {
                    wasDispatched = true;
                    return Task.FromResult(Result.Ok);
                });

        result.Should().BeSuccess();
        wasDelivered.Should().BeFalse();
        wasDispatched.Should().BeFalse();
        _subscription.Events.Last().Should().BeOfType<ManagedTrialEventScheduleEnded>();
        _subscription.Events.Last().As<ManagedTrialEventScheduleEnded>().Reason.Should()
            .Be(TrialScheduledEndingReason.TrialScheduleRemoved);
    }

    [Fact]
    public async Task
        WhenDeliverManagedTrialScheduledEventAsyncAndTrialScheduledNoLongerHasAnyItems_ThenDoesNotDeliverNorDispatchNextAndEndsSchedule()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDelivered = false;
        var wasDispatched = false;
        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1, (_, _) =>
                {
                    wasDelivered = true;
                    return Task.FromResult(Result.Ok);
                },
                (_, _, _) =>
                {
                    wasDispatched = true;
                    return Task.FromResult(Result.Ok);
                });

        result.Should().BeSuccess();
        wasDelivered.Should().BeFalse();
        wasDispatched.Should().BeFalse();
        _subscription.Events.Last().Should().BeOfType<ManagedTrialEventScheduleEnded>();
        _subscription.Events.Last().As<ManagedTrialEventScheduleEnded>().Reason.Should()
            .Be(TrialScheduledEndingReason.TrialScheduleRemoved);
    }

    [Fact]
    public async Task
        WhenDeliverManagedTrialScheduledEventAsyncAndDispatchedEventNoLongerExistsInSchedule_ThenDoesNotDeliverNorDispatchNext()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid1", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var event2 = TrialScheduledEvent.Create(1, "anid2", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event2]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDelivered = false;
        var wasDispatched = false;
        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1, (_, _) =>
                {
                    wasDelivered = true;
                    return Task.FromResult(Result.Ok);
                },
                (_, _, _) =>
                {
                    wasDispatched = true;
                    return Task.FromResult(Result.Ok);
                });

        result.Should().BeSuccess();
        wasDelivered.Should().BeFalse();
        wasDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task
        WhenDeliverManagedTrialScheduledEventAsyncAndEventConditionNoLongerMatchesTrialState_ThenDoesNotDeliverNorDispatchNext()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid1", TrialScheduledEventTrack.Expired,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDelivered = false;
        var wasDispatched = false;
        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1, (_, _) =>
                {
                    wasDelivered = true;
                    return Task.FromResult(Result.Ok);
                },
                (_, _, _) =>
                {
                    wasDispatched = true;
                    return Task.FromResult(Result.Ok);
                });

        result.Should().BeSuccess();
        wasDelivered.Should().BeFalse();
        wasDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task
        WhenDeliverManagedTrialScheduledEventAsyncForTrialStateButLastScheduledEventForActive_ThenDeliversAndDoesNotDispatch()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid1", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDelivered = false;
        var wasDispatched = false;
        TrialScheduledEvent? deliveredEvent = null;
        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1, (_, @event) =>
                {
                    wasDelivered = true;
                    deliveredEvent = @event;
                    return Task.FromResult(Result.Ok);
                },
                (_, _, _) =>
                {
                    wasDispatched = true;
                    return Task.FromResult(Result.Ok);
                });

        result.Should().BeSuccess();
        wasDelivered.Should().BeTrue();
        deliveredEvent.Should().Be(event1);
        wasDispatched.Should().BeFalse();
        _subscription.Events.Last().Should().BeOfType<ManagedTrialScheduledEventAdded>();
    }

    [Fact]
    public async Task
        WhenDeliverManagedTrialScheduledEventAsyncForTrialStateButLastScheduledEventForExpired_ThenDeliversAndDoesNotDispatch()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid1", TrialScheduledEventTrack.Expired,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow.SubtractDays(10), 1).Value;
        trial = trial.ExpireTrial().Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDelivered = false;
        var wasDispatched = false;
        TrialScheduledEvent? deliveredEvent = null;
        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1, (_, @event) =>
                {
                    wasDelivered = true;
                    deliveredEvent = @event;
                    return Task.FromResult(Result.Ok);
                },
                (_, _, _) =>
                {
                    wasDispatched = true;
                    return Task.FromResult(Result.Ok);
                });

        result.Should().BeSuccess();
        wasDelivered.Should().BeTrue();
        deliveredEvent.Should().Be(event1);
        wasDispatched.Should().BeFalse();
        _subscription.Events.Last().Should().BeOfType<ManagedTrialScheduledEventAdded>();
    }

    [Fact]
    public async Task
        WhenDeliverManagedTrialScheduledEventAsyncForTrialStateButLastScheduledEventForConverted_ThenDeliversAndDoesNotDispatchAndEndsSchedule()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid1", TrialScheduledEventTrack.Converted,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
        trial = trial.ConvertTrial().Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDelivered = false;
        var wasDispatched = false;
        TrialScheduledEvent? deliveredEvent = null;
        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1, (_, @event) =>
                {
                    wasDelivered = true;
                    deliveredEvent = @event;
                    return Task.FromResult(Result.Ok);
                },
                (_, _, _) =>
                {
                    wasDispatched = true;
                    return Task.FromResult(Result.Ok);
                });

        result.Should().BeSuccess();
        wasDelivered.Should().BeTrue();
        deliveredEvent.Should().Be(event1);
        wasDispatched.Should().BeFalse();
        _subscription.Events.Count.Should().Be(4);
        _subscription.Events[2].Should().BeOfType<ManagedTrialScheduledEventAdded>();
        _subscription.Events.Last().Should().BeOfType<ManagedTrialEventScheduleEnded>();
        _subscription.Events.Last().As<ManagedTrialEventScheduleEnded>().Reason.Should()
            .Be(TrialScheduledEndingReason.NoMoreEvents);
    }

    [Fact]
    public async Task
        WhenDeliverManagedTrialScheduledEventAsyncAndStillMatchesTrialAndHasAnotherScheduledEvent_ThenDeliversAndDispatchesNextEvent()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid1", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var event2 = TrialScheduledEvent.Create(1, "anid2", TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1, event2]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDelivered = false;
        var wasDispatched = false;
        TrialScheduledEvent? deliveredEvent = null;
        TrialScheduledEvent? dispatchedEvent = null;
        var result =
            await _subscription.DeliverManagedTrialScheduledEventAsync(_interpreter.Object, event1, (_, @event) =>
                {
                    wasDelivered = true;
                    deliveredEvent = @event;
                    return Task.FromResult(Result.Ok);
                },
                (_, @event, _) =>
                {
                    wasDispatched = true;
                    dispatchedEvent = @event;
                    return Task.FromResult(Result.Ok);
                });

        result.Should().BeSuccess();
        wasDelivered.Should().BeTrue();
        deliveredEvent.Should().Be(event1);
        wasDispatched.Should().BeTrue();
        dispatchedEvent.Should().Be(event2);
        _subscription.Events.Last().Should().BeOfType<ManagedTrialScheduledEventAdded>();
    }

    [Fact]
    public async Task WhenExpireManagedTrialAsyncAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;

        var result =
            await _subscription.ExpireManagedTrialAsync(_interpreter.Object, trial, _ => Task.FromResult(Result.Ok),
                (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenExpireManagedTrialAsyncForSelfManagedTrial_ThenDoesNothing()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.SelfManaged
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow.SubtractDays(10), 1).Value;

        var wasExpired = false;
        var wasDispatched = false;
        var result = await _subscription.ExpireManagedTrialAsync(_interpreter.Object, trial,
            _ =>
            {
                wasExpired = true;
                return Task.FromResult(Result.Ok);
            },
            (_, _, _) =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasExpired.Should().BeFalse();
        wasDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task WhenExpireManagedTrialAsyncAndTrialIsExpired_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var trial = TrialTimeline.Create(DateTime.UtcNow.SubtractDays(10), 1).Value;
        trial = trial.ExpireTrial().Value;

        var wasCalled = false;
        var result =
            await _subscription.ExpireManagedTrialAsync(_interpreter.Object, trial, _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            }, (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.SubscriptionRoot_ExpireTrial_AlreadyExpired);
        wasCalled.Should().BeFalse();
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenExpireManagedTrialAsyncAndTrialNotExpirable_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;

        var wasCalled = false;
        var result =
            await _subscription.ExpireManagedTrialAsync(_interpreter.Object, trial, _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            }, (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.SubscriptionRoot_ExpireTrial_NotExpirable);
        wasCalled.Should().BeFalse();
        _subscription.Events.Last().Should().BeOfType<ProviderChanged>();
    }

    [Fact]
    public async Task WhenExpireManagedTrialAsyncAndExpiredAndHasNoScheduledExpiredEvents_ThenExpiresTrial()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var schedule = TrialEventSchedule.Create([]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow.SubtractDays(10), 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasExpired = false;
        var wasDispatched = false;
        var result =
            await _subscription.ExpireManagedTrialAsync(_interpreter.Object, trial, _ =>
            {
                wasExpired = true;
                return Task.FromResult(Result.Ok);
            }, (_, _, _) =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasExpired.Should().BeTrue();
        wasDispatched.Should().BeFalse();
        _subscription.ManagedTrial.Value.IsExpired.Should().BeTrue();
        _subscription.Events.Last().Should().BeOfType<ManagedTrialExpired>();
    }

    [Fact]
    public async Task
        WhenExpireManagedTrialAsyncAndExpirableAndHasScheduledExpiredEvent_ThenExpiresTrialAndDispatchesFirstExpiredEvent()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var event1 = TrialScheduledEvent.Create(1, "anid2", TrialScheduledEventTrack.Expired,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var schedule = TrialEventSchedule.Create([event1]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow.SubtractDays(10), 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasExpired = false;
        var wasDispatched = false;
        TrialScheduledEvent? dispatchedEvent = null;
        var result =
            await _subscription.ExpireManagedTrialAsync(_interpreter.Object, trial, _ =>
            {
                wasExpired = true;
                return Task.FromResult(Result.Ok);
            }, (_, @event, _) =>
            {
                wasDispatched = true;
                dispatchedEvent = @event;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasExpired.Should().BeTrue();
        wasDispatched.Should().BeTrue();
        dispatchedEvent.Should().Be(event1);
        _subscription.Events.Last().Should().BeOfType<ManagedTrialExpired>();
    }

    [Fact]
    public async Task WhenHandleManagedTrialExpiredSignalAsyncForSelfManagedTrial_ThenDoesNothing()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.SelfManaged
            });

        var wasSignalDispatched = false;
        var wasExpired = false;
        var wasEventDispatched = false;
        var result = await _subscription.HandleManagedTrialExpiredSignalAsync(_interpreter.Object,
            _ =>
            {
                wasSignalDispatched = true;
                return Task.FromResult(Result.Ok);
            },
            _ =>
            {
                wasExpired = true;
                return Task.FromResult(Result.Ok);
            },
            (_, _, _) =>
            {
                wasEventDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasSignalDispatched.Should().BeFalse();
        wasExpired.Should().BeFalse();
        wasEventDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task WhenHandleManagedTrialExpiredSignalAsyncAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result = await _subscription.HandleManagedTrialExpiredSignalAsync(_interpreter.Object,
            _ => Task.FromResult(Result.Ok),
            _ => Task.FromResult(Result.Ok),
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenHandleManagedTrialExpiredSignalAsyncAndNoManagedTrial_ThenDoesNothing()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var wasSignalDispatched = false;
        var wasExpired = false;
        var result = await _subscription.HandleManagedTrialExpiredSignalAsync(_interpreter.Object,
            _ =>
            {
                wasSignalDispatched = true;
                return Task.FromResult(Result.Ok);
            },
            _ =>
            {
                wasExpired = true;
                return Task.FromResult(Result.Ok);
            },
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        wasSignalDispatched.Should().BeFalse();
        wasExpired.Should().BeFalse();
    }

    [Fact]
    public async Task WhenHandleManagedTrialExpiredSignalAsyncAndManagedTrialIsConverted_ThenDoesNothing()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 1).Value;
        trial = trial.ConvertTrial().Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasSignalDispatched = false;
        var wasExpired = false;
        var result = await _subscription.HandleManagedTrialExpiredSignalAsync(_interpreter.Object,
            _ =>
            {
                wasSignalDispatched = true;
                return Task.FromResult(Result.Ok);
            },
            _ =>
            {
                wasExpired = true;
                return Task.FromResult(Result.Ok);
            },
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        wasSignalDispatched.Should().BeFalse();
        wasExpired.Should().BeFalse();
    }

    [Fact]
    public async Task WhenHandleManagedTrialExpiredSignalAsyncAndManagedTrialIsExpired_ThenDoesNothing()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var trial = TrialTimeline.Create(DateTime.UtcNow.SubtractDays(10), 1).Value;
        trial = trial.ExpireTrial().Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasSignalDispatched = false;
        var wasExpired = false;
        var result = await _subscription.HandleManagedTrialExpiredSignalAsync(_interpreter.Object,
            _ =>
            {
                wasSignalDispatched = true;
                return Task.FromResult(Result.Ok);
            },
            _ =>
            {
                wasExpired = true;
                return Task.FromResult(Result.Ok);
            },
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        wasSignalDispatched.Should().BeFalse();
        wasExpired.Should().BeFalse();
    }

    [Fact]
    public async Task WhenHandleManagedTrialExpiredSignalAsyncAndManagedTrialIsExpirable_ThenExpiresTrial()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var schedule = TrialEventSchedule.Create([]).Value;
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.RequiresManaged,
                ManagedTrialSchedule = schedule
            });
        var trial = TrialTimeline.Create(DateTime.UtcNow.SubtractDays(10), 1).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasSignalDispatched = false;
        var wasExpired = false;
        var result = await _subscription.HandleManagedTrialExpiredSignalAsync(_interpreter.Object,
            _ =>
            {
                wasSignalDispatched = true;
                return Task.FromResult(Result.Ok);
            },
            _ =>
            {
                wasExpired = true;
                return Task.FromResult(Result.Ok);
            },
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        wasExpired.Should().BeTrue();
        wasSignalDispatched.Should().BeFalse();
        _subscription.ManagedTrial.Value.IsExpired.Should().BeTrue();
        _subscription.Events.Last().Should().BeOfType<ManagedTrialExpired>();
    }

    [Fact]
    public async Task WhenHandleManagedTrialExpiredSignalAsyncAndManagedTrialIsNotExpirable_ThenDispatchesNextSignal()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 14).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasSignalDispatched = false;
        var wasExpired = false;
        var result = await _subscription.HandleManagedTrialExpiredSignalAsync(_interpreter.Object,
            _ =>
            {
                wasSignalDispatched = true;
                return Task.FromResult(Result.Ok);
            },
            _ =>
            {
                wasExpired = true;
                return Task.FromResult(Result.Ok);
            },
            (_, _, _) => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        wasSignalDispatched.Should().BeTrue();
        wasExpired.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDispatchManagedTrialFirstExpirySignalAsyncAndNotInstalledProvider_ThenReturnsError()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bsi => bsi.ProviderName)
            .Returns("anotherprovidername");

        var result = await _subscription.DispatchManagedTrialFirstExpirySignalAsync(_interpreter.Object,
            _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.SubscriptionRoot_InstalledProviderMismatch);
    }

    [Fact]
    public async Task WhenDispatchManagedTrialFirstExpirySignalAsyncForSelfManagedTrial_ThenDoesNothing()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        _interpreter.Setup(bsi => bsi.Capabilities)
            .Returns(new BillingProviderCapabilities
            {
                TrialManagement = TrialManagementOptions.SelfManaged
            });

        var wasDispatched = false;
        var result = await _subscription.DispatchManagedTrialFirstExpirySignalAsync(_interpreter.Object,
            _ =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDispatchManagedTrialFirstExpirySignalAsyncAndNoTrial_ThenDoesNothing()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);

        var wasDispatched = false;
        var result = await _subscription.DispatchManagedTrialFirstExpirySignalAsync(_interpreter.Object,
            _ =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDispatchManagedTrialFirstExpirySignalAsync_ThenDispatchesSignal()
    {
        var initialProvider = BillingProvider.Create("aprovidername", new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }).Value;
        _subscription.SetProvider(initialProvider, "abuyerid".ToId(), _interpreter.Object);
        var trial = TrialTimeline.Create(DateTime.UtcNow, 14).Value;
#if TESTINGONLY
        _subscription.TestingOnly_SetManagedTrial(trial);
#endif

        var wasDispatched = false;
        var result = await _subscription.DispatchManagedTrialFirstExpirySignalAsync(_interpreter.Object,
            _ =>
            {
                wasDispatched = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasDispatched.Should().BeTrue();
    }
}