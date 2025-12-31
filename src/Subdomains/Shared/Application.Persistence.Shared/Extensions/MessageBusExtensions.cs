using Application.Persistence.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Application.Resources.Shared;
using Domain.Common.Extensions;

namespace Application.Persistence.Shared.Extensions;

public static class MessageBusExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="EventStreamChangeEvent" /> to a <see cref="DomainEventingMessage" /> for
    ///     transport over a message bus
    /// </summary>
    public static DomainEventingMessage ToDomainEventingMessage(this EventStreamChangeEvent changeEvent)
    {
        var @event = changeEvent.OriginalEvent;
        var eventType = changeEvent.EventType;
        var aggregateType = changeEvent.RootAggregateType;

        return new DomainEventingMessage
        {
            Event = new DomainEventNotification
            {
                Id = changeEvent.Id,
                LastPersistedAtUtc = changeEvent.LastPersistedAtUtc,
                AggregateTypeFullName = aggregateType.AssemblyQualifiedName!,
                EventJsonData = @event.ToEventJson(),
                EventTypeFullName = eventType.AssemblyQualifiedName!,
                StreamName = changeEvent.StreamName,
                Version = changeEvent.Version
            }
        };
    }
}