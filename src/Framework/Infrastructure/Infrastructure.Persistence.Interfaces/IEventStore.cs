using Common;
using Domain.Interfaces.Entities;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines read/write access to streams of events of an aggregate root to and from an event store
///     (e.g. a database (relational or not), or an event store)
/// </summary>
public interface IEventStore
{
    /// <summary>
    ///     Adds the specified events to the event store for the specified aggregate root
    /// </summary>
    Task<Result<string, Error>> AddEventsAsync<TAggregateRoot>(string aggregateRootId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
        where TAggregateRoot : class, IEventingAggregateRoot;

#if TESTINGONLY
    /// <summary>
    ///     Destroys all the events for the specified aggregate root
    /// </summary>
    Task<Result<Error>> DestroyAllAsync<TAggregateRoot>(CancellationToken cancellationToken)
        where TAggregateRoot : class, IEventingAggregateRoot;
#endif

    /// <summary>
    ///     Returns the event stream for the specified aggregate root
    /// </summary>
    Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync<TAggregateRoot>(
        string aggregateRootId,
        IEventSourcedChangeEventMigrator eventMigrator, CancellationToken cancellationToken)
        where TAggregateRoot : class, IEventingAggregateRoot;
}