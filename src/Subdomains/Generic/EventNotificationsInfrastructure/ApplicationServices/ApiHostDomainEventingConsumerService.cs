using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Common.Recording;
using Infrastructure.Eventing.Common.Extensions;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace EventNotificationsInfrastructure.ApplicationServices;

/// <summary>
///     Provides a service that notified consumers of domain events notifications.
///     Note: This service receives domain event notifications from the message bus, and notifies the registered consumers
///     of them in this AppDomain.
///     Warning: These notification messages, may have been on the message bus already, before this AppDomain was started.
///     It is possible that when this AppDomain starts, the original event classes may have changed, and so, there is a
///     remote chance that when the notification message is deserialized back into the original event class, that class no
///     longer exists, or has breaking changes in it.
/// </summary>
public class ApiHostDomainEventingConsumerService : IDomainEventingConsumerService
{
    private readonly IReadOnlyList<IDomainEventingSubscribingConsumer> _consumers;
    private readonly IDomainEventingSubscriberService _subscriberService;

    public ApiHostDomainEventingConsumerService(IRecorder recorder,
        IEnumerable<IDomainEventNotificationConsumer> consumers,
        IDomainEventingSubscriberService subscriberService) : this(subscriberService,
        WrapConsumers(recorder, subscriberService, consumers))
    {
    }

    internal ApiHostDomainEventingConsumerService(IDomainEventingSubscriberService subscriberService,
        IReadOnlyList<IDomainEventingSubscribingConsumer> consumers)
    {
        _subscriberService = subscriberService;
        _consumers = consumers;
    }

    public async Task<Result<Error>> NotifySubscriberAsync(string subscriptionName,
        DomainEventNotification domainEventNotification,
        CancellationToken cancellationToken)
    {
        var consumer =
            _consumers.FirstOrDefault(consumer => consumer.SubscriptionName.EqualsIgnoreCase(subscriptionName));
        if (consumer.NotExists())
        {
            throw new InvalidOperationException(Resources
                .ApiHostDomainEventingConsumerService_NotifySubscriberAsync_MissingConsumer
                .Format(subscriptionName));
        }

        return await consumer.NotifyAsync(domainEventNotification, cancellationToken);
    }

    public IReadOnlyList<string> SubscriptionNames => _subscriberService.SubscriptionNames;

    private static List<IDomainEventingSubscribingConsumer> WrapConsumers(IRecorder recorder,
        IDomainEventingSubscriberService subscriberService, IEnumerable<IDomainEventNotificationConsumer> consumers)
    {
        var injectedConsumers = consumers.ToList();
        var registeredConsumers = subscriberService.Consumers;
        var foundConsumersTypes = injectedConsumers.Select(consumer => consumer.GetType()).ToList();
        var registeredConsumerTypes = registeredConsumers.Select(consumer => consumer.Key).ToList();
        var missingFromRegistration = foundConsumersTypes.Except(registeredConsumerTypes).ToList();
        var missingFromInjection = registeredConsumerTypes.Except(foundConsumersTypes).ToList();
        if (missingFromRegistration.HasAny())
        {
            var notRegistered = missingFromRegistration.Select(type => type.FullName).Join(",");
            throw new InvalidOperationException(
                Resources.ApiHostDomainEventingConsumerService_WrapConsumers_MissingFromRegistration.Format(
                    notRegistered));
        }

        if (missingFromInjection.HasAny())
        {
            var notInjected = missingFromInjection.Select(type => type.FullName).Join(",");
            throw new InvalidOperationException(
                Resources.ApiHostDomainEventingConsumerService_WrapConsumers_MissingFromInjection.Format(notInjected));
        }

        return injectedConsumers
            .Select(consumer =>
            {
                var registeredConsumer = registeredConsumers.Single(rc => rc.Key == consumer.GetType());
                return new ApiHostDomainEventingSubscribingConsumer(recorder, registeredConsumer.Value,
                    consumer);
            })
            .ToList<IDomainEventingSubscribingConsumer>();
    }

    public interface IDomainEventingSubscribingConsumer
    {
        string SubscriptionName { get; }

        Task<Result<Error>> NotifyAsync(DomainEventNotification domainEventNotification,
            CancellationToken cancellationToken);
    }

    /// <summary>
    ///     Provides message bus subscriber for domain events notifications
    /// </summary>
    internal class ApiHostDomainEventingSubscribingConsumer : IDomainEventingSubscribingConsumer
    {
        private readonly IDomainEventNotificationConsumer _consumer;
        private readonly IRecorder _recorder;

        public ApiHostDomainEventingSubscribingConsumer(IRecorder recorder, string subscriptionName,
            IDomainEventNotificationConsumer consumer)
        {
            _recorder = recorder;
            SubscriptionName = subscriptionName;
            _consumer = consumer;
        }

        public async Task<Result<Error>> NotifyAsync(DomainEventNotification domainEventNotification,
            CancellationToken cancellationToken)
        {
            var converted = domainEventNotification.ToDomainEvent();
            if (converted.IsFailure)
            {
                return converted.Error;
            }

            var @event = converted.Value;
            var notified = await _consumer.NotifyAsync(@event, cancellationToken);
            if (notified.IsFailure)
            {
                var consumerName = _consumer.GetType().Name;
                var eventId = @event.RootId;
                var eventName = domainEventNotification.EventTypeFullName;
                var ex = notified.Error.ToException<InvalidOperationException>();
                _recorder.Crash(null, CrashLevel.Critical, ex,
                    "Consumer {Consumer} failed to process event {EventId} ({EventType})",
                    consumerName, eventId, eventName);
                return notified.Error.Wrap(ErrorCode.Unexpected,
                    Resources.DomainEventingSubscriber_ConsumerFailed.Format(consumerName, eventId, eventName));
            }

            return Result.Ok;
        }

        public string SubscriptionName { get; }
    }
}