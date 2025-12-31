using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access to individual <see cref="IEventingAggregateRoot" /> DDD aggregate roots for [CQRS]
///     commands that use event sourced persistence
/// </summary>
public class EventSourcingDddCommandStore<TAggregateRoot> : IEventSourcingDddCommandStore<TAggregateRoot>
    where TAggregateRoot : class, IEventingAggregateRoot
{
    private readonly IDomainFactory _domainFactory;
    private readonly IEventStore _eventStore;
    private readonly IEventSourcedChangeEventMigrator _migrator;
    private readonly IRecorder _recorder;

    public EventSourcingDddCommandStore(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcedChangeEventMigrator migrator, IEventStore eventStore)
    {
        InstanceId = Guid.NewGuid();
        _recorder = recorder;
        _eventStore = eventStore;
        _domainFactory = domainFactory;
        _migrator = migrator;
    }

    public Guid InstanceId { get; }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _eventStore.DestroyAllAsync<TAggregateRoot>(cancellationToken);
        if (deleted.IsSuccessful)
        {
            var aggregateName = typeof(TAggregateRoot).FullName!;
            _recorder.TraceDebug(null, "All events were deleted for the event stream {Aggregate} in the {Store} store",
                aggregateName, _eventStore.GetType().Name);
        }

        return deleted;
    }
#endif

    public async Task<Result<TAggregateRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        return await _recorder.MeasureWithDuration<Task<Result<TAggregateRoot, Error>>>(null,
            "EventSourcingDddCommandStore<TAggregateRoot>.LoadAsync", async context =>
            {
                var eventStream =
                    await _eventStore.GetEventStreamAsync<TAggregateRoot>(id, _migrator,
                        cancellationToken);
                if (eventStream.IsFailure)
                {
                    return eventStream.Error;
                }

                var events = eventStream.Value;
                if (events.HasNone())
                {
                    return Error.EntityNotFound();
                }

                var verified = VerifyEventsIntegrity(events);
                if (verified.IsFailure)
                {
                    return verified.Error;
                }

                AddMeasurementData(context, events);

                if (IsTombstoned(events))
                {
                    return Error.EntityDeleted(Resources.EventSourcingDddCommandStore_StreamTombstoned);
                }

                var lastPersistedAtUtc = events.Last().LastPersistedAtUtc;
                var aggregate = RehydrateAggregateRoot(id, lastPersistedAtUtc);
                var loaded = aggregate.LoadChanges(events);
                if (loaded.IsFailure)
                {
                    return loaded.Error;
                }

                return aggregate;
            });

        void AddMeasurementData(IDictionary<string, object> context,
            IReadOnlyCollection<EventSourcedChangeEvent> events)
        {
            context.Add("AggregateType", typeof(TAggregateRoot).FullName!);
            context.Add("RootId", id);
            context.Add("EventCount", events.Count.ToString());
        }
    }

    public event EventStreamChangedAsync<EventStreamChangedArgs>? OnEventStreamChanged;

    public async Task<Result<Error>> SaveAsync(TAggregateRoot aggregate, CancellationToken cancellationToken)
    {
        if (aggregate.Id.IsEmpty())
        {
            return Error.EntityExists(Resources.EventSourcingDddCommandStore_SaveWithAggregateIdMissing);
        }

        var published = await this.SaveAndPublishChangesAsync(aggregate, OnEventStreamChanged,
            (root, changedEvents, token) =>
                _eventStore.AddEventsAsync<TAggregateRoot>(root.Id.Value, changedEvents, token),
            cancellationToken);
        if (published.IsFailure)
        {
            return published.Error;
        }

        return Result.Ok;
    }

    private Result<Error> VerifyEventsIntegrity(IReadOnlyList<EventSourcedChangeEvent> events)
    {
        if (events.HasNone())
        {
            return Result.Ok;
        }

        var firstInvalidEvent = events
            .Cast<EventSourcedChangeEvent?>() // Since we have a list of struct
            .FirstOrDefault(evt => evt.HasValue && (evt.Value.AggregateType.NotExists()
                                                    || evt.Value.OriginalEvent.NotExists()
                                                    || evt.Value.EventType.NotExists()));
        if (firstInvalidEvent.NotExists())
        {
            return Result.Ok;
        }

        var storeType = _eventStore.GetType().Name;
        return Error.Unexpected(
            Resources.EventSourcingDddCommandStore_LoadWithEventDataMissing.Format(storeType,
                firstInvalidEvent.ToJson()!));
    }

    private static bool IsTombstoned(IEnumerable<EventSourcedChangeEvent> events)
    {
        var lastEvent = events.Last();
        return lastEvent.IsTombstone;
    }

    private TAggregateRoot RehydrateAggregateRoot(ISingleValueObject<string> id, Optional<DateTime> lastPersistedAtUtc)
    {
        return (TAggregateRoot)_domainFactory.RehydrateAggregateRoot(typeof(TAggregateRoot),
            new HydrationProperties
            {
                { nameof(IEventingAggregateRoot.Id), id.ToOptional<object>() },
                {
                    nameof(IEventingAggregateRoot.LastPersistedAtUtc),
                    lastPersistedAtUtc.ValueOrDefault.ToOptional<object>()
                }
            });
    }
}