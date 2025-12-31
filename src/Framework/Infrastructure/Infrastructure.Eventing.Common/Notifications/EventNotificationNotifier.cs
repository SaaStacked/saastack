using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a round-robin notifier of domain events to registered consumers, in-process and synchronously
/// </summary>
public sealed class EventNotificationNotifier : IEventNotificationNotifier, IDisposable
{
    private readonly IDomainEventConsumerRelay _consumerRelay;
    private readonly IEventNotificationMessageBroker _messageBroker;
    private readonly IRecorder _recorder;

    public EventNotificationNotifier(IRecorder recorder, List<IEventNotificationRegistration> registrations,
        IDomainEventConsumerRelay consumerRelay,
        IEventNotificationMessageBroker messageBroker)
    {
        _recorder = recorder;
        Registrations = registrations;
        _consumerRelay = consumerRelay;
        _messageBroker = messageBroker;
    }

    ~EventNotificationNotifier()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        if (Registrations.Any())
        {
            foreach (var pair in Registrations)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                (pair.IntegrationEventTranslator as IDisposable)?.Dispose();
            }
        }
    }

    public IReadOnlyList<IEventNotificationRegistration> Registrations { get; }

    public async Task<Result<Error>> WriteEventStreamAsync(string streamName, List<EventStreamChangeEvent> eventStream,
        CancellationToken cancellationToken)
    {
        streamName.ThrowIfNotValuedParameter(nameof(streamName));

        if (eventStream.HasNone())
        {
            return Result.Ok;
        }

        var published = await RelayEventStreamToAllConsumersInOrderAsync(eventStream, cancellationToken);
        if (published.IsFailure)
        {
            return published.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> RelayEventStreamToAllConsumersInOrderAsync(
        List<EventStreamChangeEvent> eventStream, CancellationToken cancellationToken)
    {
        foreach (var changeEvent in eventStream)
        {
            var domainEventRelayed =
                await _consumerRelay.RelayDomainEventAsync(changeEvent, cancellationToken);
            if (domainEventRelayed.IsFailure)
            {
                var eventId = changeEvent.OriginalEvent.RootId;
                return domainEventRelayed.Error
                    .Wrap(ErrorCode.Unexpected, Resources.EventNotificationNotifier_ConsumerError.Format(
                        _consumerRelay.GetType().Name, eventId, changeEvent.EventType.AssemblyQualifiedName!));
            }

            var integrationEventRelayed =
                await RelayIntegrationEventToBrokerAsync(changeEvent, cancellationToken);
            if (integrationEventRelayed.IsFailure)
            {
                return integrationEventRelayed.Error;
            }
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> RelayIntegrationEventToBrokerAsync(EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken)
    {
        if (Registrations.HasNone())
        {
            return Result.Ok;
        }

        var rootAggregateType = changeEvent.RootAggregateType;
        var translator = GetTranslatorForStream(Registrations, rootAggregateType);
        if (translator.NotExists())
        {
            // Nothing to publish
            return Result.Ok;
        }

        var @event = changeEvent.OriginalEvent;
        var published = await translator.TranslateAsync(@event, cancellationToken);
        if (published.IsFailure)
        {
            return published.Error.Wrap(ErrorCode.Unexpected,
                Resources.EventNotificationNotifier_TranslatorError.Format(
                    translator.GetType().Name,
                    @event.RootId, changeEvent.EventType.AssemblyQualifiedName!));
        }

        var publishedEvent = published.Value;
        if (!publishedEvent.HasValue)
        {
            _recorder.TraceDebug(null,
                "The producer '{Producer}' chose not publish the integration event '{Event}' with event type '{Type}'",
                translator.GetType().Name, changeEvent.Id, changeEvent.EventType.AssemblyQualifiedName!);
            return Result.Ok;
        }

        var integrationEvent = publishedEvent.Value;
        var brokered = await _messageBroker.PublishAsync(integrationEvent, cancellationToken);
        if (brokered.IsFailure)
        {
            return brokered.Error.Wrap(ErrorCode.Unexpected,
                Resources.EventNotificationNotifier_MessageBrokerError.Format(
                    _messageBroker.GetType().Name, @event.RootId, changeEvent.EventType.AssemblyQualifiedName!));
        }

        return Result.Ok;
    }

    private static IIntegrationEventNotificationTranslator? GetTranslatorForStream(
        IEnumerable<IEventNotificationRegistration> registrations, Type aggregateType)
    {
        var registration = registrations
            .FirstOrDefault(prj => prj.IntegrationEventTranslator.RootAggregateType == aggregateType);

        return registration.Exists()
            ? registration.IntegrationEventTranslator
            : null;
    }
}