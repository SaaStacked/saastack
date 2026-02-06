#if TESTINGONLY
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.External.Persistence.Common;
using Infrastructure.External.Persistence.Common.Extensions;
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.External.Persistence.TestingOnly.ApplicationServices;

partial class InProcessInMemStore : IEventStore
{
    private readonly Dictionary<string, Dictionary<string, HydrationProperties>> _events = new();

    public async Task<Result<string, Error>> AddEventsAsync<TAggregateRoot>(string aggregateRootId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
        where TAggregateRoot : class, IEventingAggregateRoot
    {
        aggregateRootId.ThrowIfNotValuedParameter(nameof(aggregateRootId), Resources.AnyStore_MissingAggregateRootId);

        var streamName = aggregateRootId.GetEventStreamName<TAggregateRoot>();

        var latestStoredEvent = await GetLatestEventAsync<TAggregateRoot>(streamName);
        var latestStoredEventVersion = latestStoredEvent.HasValue
            ? latestStoredEvent.Value.Version.ToOptional()
            : Optional<int>.None;
        var @checked =
            this.VerifyContiguousCheck(streamName, latestStoredEventVersion, Enumerable.First(events).Version);
        if (@checked.IsFailure)
        {
            return @checked.Error;
        }

        var containerName = EventStoreExtensions.GetAggregateName<TAggregateRoot>();
        foreach (var @event in events)
        {
            var entity = @event.ToJsonRecord<TAggregateRoot>(streamName);
            var version = @event.Version;

            if (!_events.ContainsKey(containerName))
            {
                _events.Add(containerName, new Dictionary<string, HydrationProperties>());
            }

            try
            {
                var stream = _events[containerName];
                stream.Add(entity.Id, entity.ToHydrationProperties());
            }
            catch (ArgumentException)
            {
                var storeType = GetType().Name;
                return Error.EntityExists(
                    Resources.EventStore_Concurrency_StreamCollisionDetected
                        .Format(storeType, streamName, version));
            }
        }

        return streamName;
    }

#if TESTINGONLY
    Task<Result<Error>> IEventStore.DestroyAllAsync<TAggregateRoot>(CancellationToken cancellationToken)
    {
        var containerName = EventStoreExtensions.GetAggregateName<TAggregateRoot>();

        _events.Remove(containerName);

        return Task.FromResult(Result.Ok);
    }
#endif

    public async Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync<TAggregateRoot>(
        string aggregateRootId, IEventSourcedChangeEventMigrator eventMigrator, CancellationToken cancellationToken)
        where TAggregateRoot : class, IEventingAggregateRoot
    {
        aggregateRootId.ThrowIfNotValuedParameter(nameof(aggregateRootId), Resources.AnyStore_MissingAggregateRootId);

        var streamName = aggregateRootId.GetEventStreamName<TAggregateRoot>();
        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version);

        //HACK: we use QueryEntity.ToDto() here, since EventSourcedChangeEvent can be rehydrated without a IDomainFactory 
        var queries = await QueryEventStoresAsync<EventStoreEntity, TAggregateRoot>(query);
        var events = queries
            .ConvertAll(entity => entity.FromJsonRecord<TAggregateRoot>(eventMigrator));

        return events;
    }

    private async Task<List<QueryEntity>> QueryEventStoresAsync<TQueryableEntity, TAggregateRoot>(
        QueryClause<TQueryableEntity> query)
        where TQueryableEntity : IQueryableEntity
    {
        var containerName = EventStoreExtensions.GetAggregateName<TAggregateRoot>();

        if (query.NotExists() || query.Options.IsEmpty)
        {
            return new List<QueryEntity>();
        }

        if (!_events.ContainsKey(containerName))
        {
            return new List<QueryEntity>();
        }

        var metadata = PersistedEntityMetadata.FromType<EventStoreEntity>();
        var results = await query.FetchAllIntoMemoryAsync(MaxQueryResults, metadata,
            () => Task.FromResult(_events[containerName]),
            _ => Task.FromResult(new Dictionary<string, HydrationProperties>()));

        return results.Results;
    }

    private async Task<Optional<EventStoreEntity>> GetLatestEventAsync<TAggregateRoot>(string streamName)
    {
        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version, OrderDirection.Descending)
            .Take(1);

        var queries = await QueryEventStoresAsync<EventStoreEntity, TAggregateRoot>(query);
        var latest = queries
            .FirstOrDefault();
        return latest.Exists()
            ? latest.ToDto<EventStoreEntity>()
            : Optional<EventStoreEntity>.None;
    }
}
#endif