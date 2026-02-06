#if TESTINGONLY
using AsyncKeyedLock;
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

partial class LocalMachineJsonFileStore : IEventStore
{
    private static string? _cachedContainerName;
    private static readonly AsyncKeyedLocker<string> EventStoreSection = new();

    public async Task<Result<string, Error>> AddEventsAsync<TAggregateRoot>(string aggregateRootId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
        where TAggregateRoot : class, IEventingAggregateRoot
    {
        aggregateRootId.ThrowIfNotValuedParameter(nameof(aggregateRootId), Resources.AnyStore_MissingAggregateRootId);

        var streamName = aggregateRootId.GetEventStreamName<TAggregateRoot>();

        var latestStoredEvent =
            await GetLatestEventAsync<TAggregateRoot>(aggregateRootId, streamName, cancellationToken);
        var latestStoredEventVersion = latestStoredEvent.HasValue
            ? latestStoredEvent.Value.Version.ToOptional()
            : Optional<int>.None;
        var @checked =
            this.VerifyContiguousCheck(streamName, latestStoredEventVersion, Enumerable.First(events).Version);
        if (@checked.IsFailure)
        {
            return @checked.Error;
        }

        var containerName = GetEventStoreContainerPath<TAggregateRoot>(aggregateRootId);
        var container = EnsureContainer(containerName);
        // We need to lock access to the EventStore table, to prevent concurrent writes to the table for different streams.
        using (await EventStoreSection.LockAsync(containerName, cancellationToken))
        {
            foreach (var @event in events)
            {
                var entity = @event.ToJsonRecord<TAggregateRoot>(streamName);

                var version = @event.Version;
                var filename = $"version_{version:D3}";
                var added = await container.WriteExclusiveAsync(filename, entity.ToFileProperties(), cancellationToken);
                if (added.IsFailure)
                {
                    if (added.Error.Is(ErrorCode.EntityExists))
                    {
                        var storeType = GetType().Name;
                        return Error.EntityExists(
                            Resources
                                .EventStore_Concurrency_StreamCollisionDetected
                                .Format(storeType, streamName, version));
                    }

                    return added.Error;
                }
            }
        }

        return streamName;
    }

#if TESTINGONLY
    Task<Result<Error>> IEventStore.DestroyAllAsync<TAggregateRoot>(CancellationToken cancellationToken)
    {
        var eventStore = EnsureContainer(GetEventStoreContainerPath<TAggregateRoot>());
        eventStore.Erase();

        return Task.FromResult(Result.Ok);
    }
#endif

    public async Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync<TAggregateRoot>(
        string aggregateRootId, IEventSourcedChangeEventMigrator eventMigrator,
        CancellationToken cancellationToken)
        where TAggregateRoot : class, IEventingAggregateRoot
    {
        aggregateRootId.ThrowIfNotValuedParameter(nameof(aggregateRootId), Resources.AnyStore_MissingAggregateRootId);

        var streamName = aggregateRootId.GetEventStreamName<TAggregateRoot>();

        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version);

        //HACK: we use QueryEntity.ToDto() here, since EventSourcedChangeEvent can be rehydrated without a IDomainFactory 
        var queries =
            await QueryEventStoresAsync<TAggregateRoot, EventStoreEntity>(aggregateRootId, query, cancellationToken);
        var events = queries
            .ConvertAll(entity => entity.FromJsonRecord<TAggregateRoot>(eventMigrator));

        return events;
    }

    private static string GetEventStoreContainerPath<TAggregateRoot>(string? entityId = null)
    {
        var containerName = EventStoreExtensions.GetAggregateName<TAggregateRoot>();
        if (entityId.HasValue())
        {
            return $"{DetermineEventStoreContainerName()}/{containerName}/{entityId}";
        }

        return $"{DetermineEventStoreContainerName()}/{containerName}";
    }

    private async Task<Optional<EventStoreEntity>> GetLatestEventAsync<TAggregateRoot>(string entityId,
        string streamName, CancellationToken cancellationToken)
    {
        entityId.ThrowIfNotValuedParameter(nameof(entityId));
        streamName.ThrowIfNotValuedParameter(nameof(streamName));

        var query = Query.From<EventStoreEntity>()
            .Where<string>(ee => ee.StreamName, ConditionOperator.EqualTo, streamName)
            .OrderBy(ee => ee.Version, OrderDirection.Descending)
            .Take(1);

        var queries = await QueryEventStoresAsync<TAggregateRoot, EventStoreEntity>(entityId, query, cancellationToken);
        var latest = queries
            .FirstOrDefault();

        return latest.Exists()
            ? latest.ToDto<EventStoreEntity>()
            : Optional<EventStoreEntity>.None;
    }

    private async Task<List<QueryEntity>> QueryEventStoresAsync<TAggregateRoot, TQueryableEntity>(string entityId,
        QueryClause<TQueryableEntity> query, CancellationToken cancellationToken)
        where TQueryableEntity : IQueryableEntity
    {
        if (query.NotExists() || query.Options.IsEmpty)
        {
            return new List<QueryEntity>();
        }

        var container = EnsureContainer(GetEventStoreContainerPath<TAggregateRoot>(entityId));
        if (container.IsEmpty())
        {
            return new List<QueryEntity>();
        }

        var metadata = PersistedEntityMetadata.FromType<EventStoreEntity>();
        var results = await query.FetchAllIntoMemoryAsync(MaxQueryResults, metadata,
            () => QueryPrimaryEntitiesAsync(container, metadata, cancellationToken),
            _ => Task.FromResult(new Dictionary<string, HydrationProperties>()));

        return results.Results;
    }

    private static string DetermineEventStoreContainerName()
    {
        if (_cachedContainerName.HasNoValue())
        {
            _cachedContainerName = typeof(EventStoreEntity).GetEntityNameSafe();
        }

        return _cachedContainerName;
    }
}

internal static class LocalMachineJsonFileEventStoreConversionExtensions
{
    public static EventSourcedChangeEvent FromJsonRecord<TAggregateRoot>(this QueryEntity entity,
        IEventSourcedChangeEventMigrator migrator)
        where TAggregateRoot : class, IEventingAggregateRoot
    {
        var eventId = entity.Id;
        var eventTypeFullName = entity.GetValueOrDefault(nameof(EventStoreEntity.EventTypeFullName), string.Empty)!;
        var eventJson = entity.GetValueOrDefault(nameof(EventStoreEntity.EventJsonData), string.Empty)!;
        var eventVersion = entity.GetValueOrDefault(nameof(EventStoreEntity.Version), 1);
        var lastPersistedAtUtc = entity.GetValueOrDefault(nameof(EventStoreEntity.LastPersistedAtUtc), DateTime.UtcNow);

        return migrator.FromEventStoreJson<TAggregateRoot>(eventId, eventVersion, eventJson, eventTypeFullName,
            lastPersistedAtUtc);
    }

    public static string GetEventStreamName<TAggregateRoot>(this string aggregateRootId)
    {
        var aggregateName = EventStoreExtensions.GetAggregateName<TAggregateRoot>();
        return $"{aggregateName}_{aggregateRootId}";
    }

    public static CommandEntity ToJsonRecord<TAggregateRoot>(this EventSourcedChangeEvent @event, string streamName)
        where TAggregateRoot : class, IEventingAggregateRoot
    {
        return CommandEntity.FromDto(@event.ToEventStoreEntity<TAggregateRoot>(streamName));
    }
}
#endif