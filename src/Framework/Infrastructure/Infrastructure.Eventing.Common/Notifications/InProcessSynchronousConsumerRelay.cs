using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a <see cref="IDomainEventConsumerRelay" /> that synchronously notifies consumers of domain events
///     in-process
/// </summary>
internal class InProcessSynchronousConsumerRelay : IDomainEventConsumerRelay
{
    private readonly IEnumerable<IDomainEventNotificationConsumer> _consumers;

    public InProcessSynchronousConsumerRelay(IEnumerable<IDomainEventNotificationConsumer> consumers)
    {
        _consumers = consumers;
    }

    public async Task<Result<Error>> RelayDomainEventAsync(EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken)
    {
        if (_consumers.HasNone())
        {
            return Result.Ok;
        }

        foreach (var consumer in _consumers)
        {
            var @event = changeEvent.OriginalEvent;
            var result = await consumer.NotifyAsync(@event, cancellationToken);
            if (result.IsFailure)
            {
                return result.Error
                    .Wrap(ErrorCode.Unexpected,
                        Resources.InProcessSynchronousConsumerRelay_ConsumerFailed.Format(consumer.GetType().Name,
                            @event.RootId, changeEvent.EventType.AssemblyQualifiedName!));
            }
        }

        return Result.Ok;
    }
}