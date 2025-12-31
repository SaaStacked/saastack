using Common;
using Domain.Interfaces.ValueObjects;
using QueryAny;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines a versioned <see cref="IDomainEvent" /> that is persisted as an event sourced event, in an event store,
///     used to define the change events to and from event sourced aggregates.
///     Note: We are delegating the value of <see cref="Id" /> to be the value to the <see cref="IIdentifiableEntity.Id" />
///     (using the private <see cref="Identifier" /> class) as a convenient workaround, to avoid requiring the mapping of
///     from domain properties.
/// </summary>
public struct EventSourcedChangeEvent : IIdentifiableEntity, IQueryableEntity
{
    private readonly Identifier _identifier;

    /// <summary>
    ///     Used to create <see cref="EventSourcedChangeEvent" /> instances from newly created <see cref="IDomainEvent" /> by
    ///     aggregates.
    /// </summary>
    public static Result<EventSourcedChangeEvent, Error> Create(
        Func<IIdentifiableEntity, Result<ISingleValueObject<string>, Error>> idFactory, Type aggregateType,
        IDomainEvent domainEvent, int version)
    {
        var identifier = idFactory(new EventSourcedChangeEvent());
        if (identifier.IsFailure)
        {
            return identifier.Error;
        }

        var id = identifier.Value.Value;
        return new EventSourcedChangeEvent(id, aggregateType, domainEvent)
        {
            Version = version
        };
    }

    /// <summary>
    ///     Used by Event Stores to create <see cref="EventSourcedChangeEvent" /> for rehydrating aggregates.
    /// </summary>
    public static EventSourcedChangeEvent Create(string id, Type aggregateType, IDomainEvent domainEvent, int version,
        DateTime lastPersistedAtUtc)
    {
        return new EventSourcedChangeEvent(id, aggregateType, domainEvent)
        {
            Version = version,
            LastPersistedAtUtc = lastPersistedAtUtc
        };
    }

    private EventSourcedChangeEvent(string id, Type aggregateType, IDomainEvent domainEvent)
    {
        _identifier = new Identifier(id);
        Id = id;
        AggregateType = aggregateType;
        OriginalEvent = domainEvent;

        EventType = domainEvent.GetType();
        IsTombstone = domainEvent is ITombstoneEvent;
    }

    public IDomainEvent OriginalEvent { get; private init; }

    public Type AggregateType { get; private init; }

    public Type EventType { get; private init; }

    /// <summary>
    ///     The ID of this event.
    ///     Note: This is distinct from the <see cref="IDomainEvent.RootId" /> which is the ID of the aggregate.
    /// </summary>
    public string Id { get; private init; }

    ISingleValueObject<string> IIdentifiableEntity.Id => _identifier;

    public Optional<DateTime> LastPersistedAtUtc { get; set; }

    public int Version { get; private init; }

    public bool IsTombstone { get; private init; }

    private readonly struct Identifier : ISingleValueObject<string>
    {
        public Identifier(string id)
        {
            Value = id;
        }

        public string Dehydrate()
        {
            return Value;
        }

        public string Value { get; }
    }
}