using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;
using EventNotificationsInfrastructure.ApplicationServices;
using FluentAssertions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace EventNotificationsInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class ApiHostDomainEventingSubscribingConsumerSpec
{
    private readonly ApiHostDomainEventingConsumerService.ApiHostDomainEventingSubscribingConsumer _consumer;
    private readonly Mock<IDomainEventNotificationConsumer> _notificationConsumer;
    private readonly Mock<IRecorder> _recorder;

    public ApiHostDomainEventingSubscribingConsumerSpec()
    {
        _recorder = new Mock<IRecorder>();
        _notificationConsumer = new Mock<IDomainEventNotificationConsumer>();

        _consumer = new ApiHostDomainEventingConsumerService.ApiHostDomainEventingSubscribingConsumer(_recorder.Object,
            "asubscriptionname", _notificationConsumer.Object);
    }

    [Fact]
    public void WhenConstructed_ThenSubscriptionName()
    {
        var result = _consumer.SubscriptionName;

        result.Should().Be("asubscriptionname");
    }

    [Fact]
    public async Task WhenNotifyAsyncAndConsumerReturnsError_ThenReturnsError()
    {
        var domainEvent = new TestDomainEvent();
        _notificationConsumer.Setup(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.RuleViolation("amessage"));
        var eventJson = domainEvent.ToEventJson();
        var eventNotification = new DomainEventNotification
        {
            Id = "anid",
            LastPersistedAtUtc = null,
            StreamName = "astreamname",
            Version = 1,
            AggregateTypeFullName = "anaggregatetypefullname",
            EventJsonData = eventJson,
            EventTypeFullName = typeof(TestDomainEvent).AssemblyQualifiedName!
        };

        var result = await _consumer.NotifyAsync(eventNotification, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.DomainEventingSubscriber_ConsumerFailed.Format(
                "IDomainEventNotificationConsumerProxy",
                "arootid", typeof(TestDomainEvent).AssemblyQualifiedName!)).ToString());
        _recorder.Verify(rec =>
            rec.Crash(null, CrashLevel.Critical, It.Is<Exception>(ex =>
                ex.Message.Contains("amessage")
            ), It.IsAny<string>(), It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenNotifyAsync_ThenNotifies()
    {
        var domainEvent = new TestDomainEvent
        {
            RootId = "arootid",
            AProperty = "avalue",
            OccurredUtc = DateTime.UtcNow
        };
        _notificationConsumer.Setup(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        var eventJson = domainEvent.ToEventJson();
        var eventNotification = new DomainEventNotification
        {
            Id = "anid",
            LastPersistedAtUtc = null,
            StreamName = "astreamname",
            Version = 1,
            AggregateTypeFullName = "anaggregatetypefullname",
            EventJsonData = eventJson,
            EventTypeFullName = typeof(TestDomainEvent).AssemblyQualifiedName!
        };

        var result = await _consumer.NotifyAsync(eventNotification, CancellationToken.None);

        result.Should().BeSuccess();
        _notificationConsumer.Verify(c => c.NotifyAsync(It.Is<TestDomainEvent>(evt =>
            evt.RootId == "arootid"
            && evt.OccurredUtc.HasValue()
            && evt.AProperty == "avalue"
        ), It.IsAny<CancellationToken>()));
    }
}