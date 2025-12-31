using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Interfaces.Entities;
using EventNotificationsInfrastructure.ApplicationServices;
using FluentAssertions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace EventNotificationsInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class ApiHostDomainEventingConsumerServiceSpec
{
    private readonly Mock<IDomainEventNotificationConsumer> _notificationConsumer;
    private readonly Mock<IRecorder> _recorder;
    private readonly ApiHostDomainEventingConsumerService _service;
    private readonly Mock<IDomainEventingSubscriberService> _subscriberService;
    private readonly Mock<ApiHostDomainEventingConsumerService.IDomainEventingSubscribingConsumer> _subscribingConsumer;

    public ApiHostDomainEventingConsumerServiceSpec()
    {
        _recorder = new Mock<IRecorder>();
        _notificationConsumer = new Mock<IDomainEventNotificationConsumer>();
        _subscribingConsumer = new Mock<ApiHostDomainEventingConsumerService.IDomainEventingSubscribingConsumer>();
        var consumers = new List<ApiHostDomainEventingConsumerService.IDomainEventingSubscribingConsumer>
            { _subscribingConsumer.Object };
        _subscriberService = new Mock<IDomainEventingSubscriberService>();
        _subscriberService.SetupGet(ss => ss.Consumers)
            .Returns(new Dictionary<Type, string>
            {
                { _notificationConsumer.Object.GetType(), "asubscriptionname1" }
            });

        _service =
            new ApiHostDomainEventingConsumerService(_subscriberService.Object, consumers);
    }

    [Fact]
    public void WhenConstructedAndInjectedConsumerButNotRegistered_ThenThrows()
    {
        _subscriberService.SetupGet(ss => ss.Consumers)
            .Returns(new Dictionary<Type, string>());
        var consumers = new List<IDomainEventNotificationConsumer>
            { _notificationConsumer.Object };

        FluentActions.Invoking(() =>
                new ApiHostDomainEventingConsumerService(_recorder.Object, consumers,
                    _subscriberService.Object))
            .Should().Throw<InvalidOperationException>()
            .WithMessageLike(
                Resources.ApiHostDomainEventingConsumerService_WrapConsumers_MissingFromRegistration.Format(
                    "Castle.Proxies.IDomainEventNotificationConsumerProxy"));
    }

    [Fact]
    public void WhenConstructedAndRegisteredConsumerButNotInjected_ThenThrows()
    {
        _subscriberService.SetupGet(ss => ss.Consumers)
            .Returns(new Dictionary<Type, string>
            {
                { _notificationConsumer.Object.GetType(), "asubscriptionname1" }
            });

        FluentActions.Invoking(() =>
                new ApiHostDomainEventingConsumerService(_recorder.Object, new List<IDomainEventNotificationConsumer>(),
                    _subscriberService.Object))
            .Should().Throw<InvalidOperationException>()
            .WithMessageLike(
                Resources.ApiHostDomainEventingConsumerService_WrapConsumers_MissingFromInjection.Format(
                    "Castle.Proxies.IDomainEventNotificationConsumerProxy"));
    }

    [Fact]
    public void WhenConstructed_ThenHasSubscriptionNames()
    {
        _subscriberService.SetupGet(ss => ss.SubscriptionNames)
            .Returns(["asubscriptionname1"]);

        var result = _service.SubscriptionNames;

        result.Count.Should().Be(1);
        result[0].Should().Be("asubscriptionname1");
        _subscriberService.Verify(ss => ss.SubscriptionNames);
    }

    [Fact]
    public async Task WhenNotifyAsyncAndConsumerNotFound_ThenThrows()
    {
        _subscribingConsumer.Setup(sc => sc.SubscriptionName)
            .Returns("asubscriptionname");
        var eventNotification = new DomainEventNotification
        {
            Id = "anid",
            LastPersistedAtUtc = null,
            StreamName = "astreamname",
            Version = 1,
            AggregateTypeFullName = "anaggregatetypefullname",
            EventJsonData = "adata",
            EventTypeFullName = "aneventtypefullname"
        };

        await _service.Invoking(x =>
                x.NotifySubscriberAsync("anothersubscriptionname", eventNotification, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessageLike(
                Resources.ApiHostDomainEventingConsumerService_NotifySubscriberAsync_MissingConsumer.Format(
                    "anothersubscriptionname"));
    }

    [Fact]
    public async Task WhenNotifyAsync_ThenNotifies()
    {
        _subscribingConsumer.Setup(sc => sc.SubscriptionName)
            .Returns("asubscriptionname");
        var eventNotification = new DomainEventNotification
        {
            Id = "anid",
            LastPersistedAtUtc = null,
            StreamName = "astreamname",
            Version = 1,
            AggregateTypeFullName = "anaggregatetypefullname",
            EventJsonData = "adata",
            EventTypeFullName = "aneventtypefullname"
        };

        var result =
            await _service.NotifySubscriberAsync("asubscriptionname", eventNotification, CancellationToken.None);

        result.Should().BeSuccess();
        _subscribingConsumer.Verify(sc => sc.NotifyAsync(eventNotification, It.IsAny<CancellationToken>()));
    }
}

public class TestDomainEvent : IDomainEvent
{
    public string? AProperty { get; set; }

    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; } = "arootid";
}