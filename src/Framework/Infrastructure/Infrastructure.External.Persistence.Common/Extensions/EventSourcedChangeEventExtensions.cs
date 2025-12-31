using Common;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common.ApplicationServices;

namespace Infrastructure.External.Persistence.Common.Extensions;

public static class EventSourcedChangeEventExtensions
{
    /// <summary>
    ///     Uses the specified <see cref="migrator" /> to convert the specified <see cref="eventJson" /> to an
    ///     <see cref="EventSourcedChangeEvent" />
    /// </summary>
    public static EventSourcedChangeEvent FromEventStoreJson<TAggregateRoot>(
        this IEventSourcedChangeEventMigrator migrator, string eventId, int eventVersion, string eventJson,
        string eventTypeAssemblyQualifiedName, DateTime lastPersistedAtUtc)
        where TAggregateRoot : class, IEventingAggregateRoot
    {
        var @event = migrator.Rehydrate(eventId, eventJson, eventTypeAssemblyQualifiedName).Value;
        var aggregateType = typeof(TAggregateRoot);

        return EventSourcedChangeEvent.Create(eventId, aggregateType, @event, eventVersion, lastPersistedAtUtc);
    }

    /// <summary>
    ///     Converts the specified <see cref="EventSourcedChangeEvent" /> to an
    ///     <see cref="EventStoreEntity" /> that persists the event as JSON data
    /// </summary>
    public static EventStoreEntity ToEventStoreEntity<TAggregateRoot>(this EventSourcedChangeEvent @event,
        string streamName)
        where TAggregateRoot : class, IEventingAggregateRoot
    {
        var dto = new EventStoreEntity
        {
            Id = @event.Id.ToOptional(),
            LastPersistedAtUtc = @event.LastPersistedAtUtc,
            IsDeleted = Optional<bool>.None,
            StreamName = streamName,
            Version = @event.Version,
            EventTypeFullName = @event.EventType.AssemblyQualifiedName,
            AggregateTypeFullName = typeof(TAggregateRoot).AssemblyQualifiedName,
            EventJsonData = @event.OriginalEvent.ToEventJson()
        };

        return dto;
    }
}