using Application.Persistence.Interfaces;
using Common;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;

namespace Application.Persistence.Common.Extensions;

public static class EventStreamChangeEventExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="EventStreamChangeEvent" /> to a <see cref="IDomainEvent" />
    /// </summary>
    public static Result<IDomainEvent, Error> ToDomainEvent(this EventStreamChangeEvent changeEvent,
        IEventSourcedChangeEventMigrator migrator)
    {
        var eventId = changeEvent.Id;
        var eventJson = changeEvent.OriginalEvent.ToEventJson();
        var eventTypeFullName = changeEvent.EventType.AssemblyQualifiedName!;

        return migrator.Rehydrate(eventId, eventJson, eventTypeFullName);
    }
}