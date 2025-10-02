﻿using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access to individual <see cref="IDehydratableEntity" /> DDD aggregates/entities for [CQRS]
///     commands that use snapshotting persistence
/// </summary>
public sealed class
    SnapshottingDddCommandStore<TAggregateRootOrEntity> : ISnapshottingDddCommandStore<TAggregateRootOrEntity>
    where TAggregateRootOrEntity : IDehydratableEntity
{
    private readonly string _containerName;
    private readonly IDataStore _dataStore;
    private readonly IDomainFactory _domainFactory;
    private readonly IRecorder _recorder;

    public SnapshottingDddCommandStore(IRecorder recorder, IDomainFactory domainFactory,
        IDataStore dataStore)
    {
        InstanceId = Guid.NewGuid();
        _recorder = recorder;
        _domainFactory = domainFactory;
        _dataStore = dataStore;
        _containerName = typeof(TAggregateRootOrEntity).GetEntityNameSafe();
    }

    public Guid InstanceId { get; }

    public async Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken)
    {
        return await _dataStore.CountAsync(_containerName, cancellationToken);
    }

    public async Task<Result<Error>> DeleteAsync(Identifier id, bool destroy = true,
        CancellationToken cancellationToken = default)
    {
        var retrieved = await _dataStore.RetrieveAsync(_containerName, id,
            PersistedEntityMetadata.FromType<TAggregateRootOrEntity>(),
            cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Result.Ok;
        }

        if (destroy)
        {
            var destroyed = await _dataStore.RemoveAsync(_containerName, id, cancellationToken);
            if (destroyed.IsFailure)
            {
                return destroyed.Error;
            }

            _recorder.TraceDebug(null, "Entity {Id} was destroyed in the {Store} store", id, _dataStore.GetType().Name);
            return Result.Ok;
        }

        var entity = retrieved.Value.Value;
        if (entity.IsDeleted.ValueOrDefault)
        {
            return Result.Ok;
        }

        entity.IsDeleted = true;
        var replaced = await _dataStore.ReplaceAsync(_containerName, id, entity, cancellationToken);
        if (replaced.IsFailure)
        {
            return replaced.Error;
        }

        _recorder.TraceDebug(null, "Entity {Id} was soft-deleted in the {Store} store", id, _dataStore.GetType().Name);

        return Result.Ok;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _dataStore.DestroyAllAsync(_containerName, cancellationToken);
        if (deleted.IsSuccessful)
        {
            _recorder.TraceDebug(null, "All entities were deleted from the {Store} store", _dataStore.GetType().Name);
        }

        return deleted;
    }
#endif

    public async Task<Result<Optional<TAggregateRootOrEntity>, Error>> GetAsync(Identifier id,
        bool errorIfNotFound = true, bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var retrieved = await _dataStore.RetrieveAsync(_containerName, id,
            PersistedEntityMetadata.FromType<TAggregateRootOrEntity>(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return errorIfNotFound
                ? Error.EntityNotFound()
                : Optional<TAggregateRootOrEntity>.None;
        }

        var entity = retrieved.Value.Value;
        if (entity.IsDeleted.ValueOrDefault && !includeDeleted)
        {
            return errorIfNotFound
                ? Error.EntityNotFound()
                : Optional<TAggregateRootOrEntity>.None;
        }

        _recorder.TraceDebug(null, "Entity {Id} was retrieved from the {Store} store", id, _dataStore.GetType().Name);
        return new Result<Optional<TAggregateRootOrEntity>, Error>(
            entity.ToDomainEntity<TAggregateRootOrEntity>(_domainFactory));
    }

    public event EventStreamChangedAsync<EventStreamChangedArgs>? OnEventStreamChanged;

    public async Task<Result<TAggregateRootOrEntity, Error>> ResurrectDeletedAsync(Identifier id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _dataStore.RetrieveAsync(_containerName, id,
            PersistedEntityMetadata.FromType<TAggregateRootOrEntity>(),
            cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var entity = retrieved.Value.Value;
        if (!entity.IsDeleted.ValueOrDefault)
        {
            return entity.ToDomainEntity<TAggregateRootOrEntity>(_domainFactory);
        }

        entity.IsDeleted = false;
        var replaced = await _dataStore.ReplaceAsync(_containerName, id, entity, cancellationToken);
        if (replaced.IsFailure)
        {
            return replaced.Error;
        }

        _recorder.TraceDebug(null, "Entity {Id} was resurrected in the {Store} store", id, _dataStore.GetType().Name);
        return entity.ToDomainEntity<TAggregateRootOrEntity>(_domainFactory);
    }

    public async Task<Result<TAggregateRootOrEntity, Error>> UpsertAsync(TAggregateRootOrEntity entity,
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        if (!entity.Id.Value.HasValue())
        {
            return Error.EntityNotFound(Resources.SnapshottingDddCommandStore_EntityMissingIdentifier);
        }

        var retrieved = await _dataStore.RetrieveAsync(_containerName, entity.Id.Value,
            PersistedEntityMetadata.FromType<TAggregateRootOrEntity>(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            var added = await _dataStore.AddAsync(_containerName, CommandEntity.FromDomainEntity(entity),
                cancellationToken);
            if (added.IsFailure)
            {
                return added.Error;
            }

            _recorder.TraceDebug(null, "Entity {Id} was added to the {Store} store", added.Value.Id,
                _dataStore.GetType().Name);
            return added.Value.ToDomainEntity<TAggregateRootOrEntity>(_domainFactory);
        }

        var persisted = retrieved.Value.Value;
        if (persisted.IsDeleted.ValueOrDefault
            && !includeDeleted)
        {
            return Error.EntityNotFound(Resources.SnapshottingDddCommandStore_EntityDeleted);
        }

        var merged = MergeEntities(entity, persisted);
        if (persisted.IsDeleted.ValueOrDefault)
        {
            merged.IsDeleted = false;
        }

        var replaced = await _dataStore.ReplaceAsync(_containerName, entity.Id.Value, merged, cancellationToken);
        if (replaced.IsFailure)
        {
            return replaced.Error;
        }

        _recorder.TraceDebug(null, "Entity {Id} was updated in the {Store} store", entity.Id,
            _dataStore.GetType().Name);
        var updated = replaced.Value.Value.ToDomainEntity<TAggregateRootOrEntity>(_domainFactory);

        if (entity is not IChangeEventProducingAggregateRoot aggregate)
        {
            return updated;
        }

        var published = await this.SaveAndPublishChangesAsync(aggregate, OnEventStreamChanged, (_, _, _) =>
        {
            var aggregateName = $"{_containerName}_{entity.Id}";
            return Task.FromResult<Result<string, Error>>(aggregateName);
        }, cancellationToken);
        if (published.IsFailure)
        {
            return published.Error;
        }

        return updated;
    }

    private CommandEntity MergeEntities(TAggregateRootOrEntity updated, CommandEntity persisted)
    {
        var persistedAsEntity = persisted.ToDomainEntity<TAggregateRootOrEntity>(_domainFactory);
        persistedAsEntity.PopulateWith(updated);
        return CommandEntity.FromDomainEntity(persistedAsEntity);
    }
}