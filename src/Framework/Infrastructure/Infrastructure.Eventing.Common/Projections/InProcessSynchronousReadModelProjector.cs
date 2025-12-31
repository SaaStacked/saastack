using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Eventing.Interfaces.Projections;

namespace Infrastructure.Eventing.Common.Projections;

/// <summary>
///     Provides a projector of change events to registered read models, in-process and synchronously
/// </summary>
public sealed class InProcessSynchronousReadModelProjector : IReadModelProjector, IDisposable
{
    private readonly IProjectionCheckpointRepository _checkpointStore;

    // ReSharper disable once NotAccessedField.Local
    private readonly IRecorder _recorder;

    public InProcessSynchronousReadModelProjector(IRecorder recorder, IProjectionCheckpointRepository checkpointStore,
        params IReadModelProjection[] projections)
    {
        _recorder = recorder;
        _checkpointStore = checkpointStore;
        Projections = projections;
    }

    public void Dispose()
    {
        if (Projections.Exists())
        {
            foreach (var projection in Projections)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (projection is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    public IReadOnlyList<IReadModelProjection> Projections { get; }

    public async Task<Result<Error>> WriteEventStreamAsync(string streamName, List<EventStreamChangeEvent> eventStream,
        CancellationToken cancellationToken)
    {
        streamName.ThrowIfNotValuedParameter(nameof(streamName));

        if (eventStream.HasNone())
        {
            return Result.Ok;
        }

        var streamAggregateType = Enumerable.First(eventStream).RootAggregateType;
        var firstEventVersion = Enumerable.First(eventStream).Version;
        var projection = GetProjectionForStream(Projections, streamAggregateType);
        if (projection.IsFailure)
        {
            return projection.Error;
        }

        var retrieved = await _checkpointStore.LoadCheckpointAsync(streamName, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var checkpointVersion = retrieved.Value;
        var versioned = EnsureNextVersion(streamName, checkpointVersion, firstEventVersion);
        if (versioned.IsFailure)
        {
            return versioned.Error;
        }

        var processed = 0;
        foreach (var changeEvent in SkipPreviouslyProjectedVersions(eventStream, checkpointVersion))
        {
            var projected =
                await ProjectEventAsync(projection.Value, changeEvent, cancellationToken);
            if (projected.IsFailure)
            {
                return projected.Error;
            }

            processed++;
        }

        var newCheckpoint = checkpointVersion + processed;
        var saved = await _checkpointStore.SaveCheckpointAsync(streamName, newCheckpoint, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }

    private static async Task<Result<Error>> ProjectEventAsync(IReadModelProjection projection,
        EventStreamChangeEvent changeEvent, CancellationToken cancellationToken)
    {
        var @event = changeEvent.OriginalEvent;
        var projected = await projection.ProjectEventAsync(@event, cancellationToken);
        if (projected.IsFailure)
        {
            return projected.Error.Wrap(ErrorCode.Unexpected,
                Resources.ReadModelProjector_ProjectionError_HandlerError.Format(
                    projection.GetType().Name,
                    changeEvent.Id, changeEvent.EventType.AssemblyQualifiedName!));
        }

#if TESTINGONLY
        if (!projected.Value)
        {
            //Note: this is for local development and testing only to ensure all events are configured
            return Error.Unexpected(Resources.ReadModelProjector_ProjectionError_MissingHandler.Format(
                projection.GetType().Name,
                changeEvent.Id, changeEvent.EventType.AssemblyQualifiedName!));
        }
#endif

        return Result.Ok;
    }

    private static IEnumerable<EventStreamChangeEvent> SkipPreviouslyProjectedVersions(
        IEnumerable<EventStreamChangeEvent> eventStream, int checkpoint)
    {
        return eventStream
            .Where(e => e.Version >= checkpoint);
    }

    private static Result<IReadModelProjection, Error> GetProjectionForStream(
        IEnumerable<IReadModelProjection> projections, Type aggregateType)
    {
        var projection = projections.FirstOrDefault(prj => prj.RootAggregateType == aggregateType);
        if (projection.NotExists())
        {
            return Error.RuleViolation(Resources.ReadModelProjector_ProjectionNotConfigured.Format(aggregateType));
        }

        return new Result<IReadModelProjection, Error>(projection);
    }

    private static Result<Error> EnsureNextVersion(string streamName, int checkpointVersion, int firstEventVersion)
    {
        if (firstEventVersion > checkpointVersion)
        {
            throw Error.RuleViolation(
                    Resources.ReadModelProjector_CheckpointError.Format(streamName, checkpointVersion,
                        firstEventVersion))
                .ToException<InvalidOperationException>();
        }

        return Result.Ok;
    }
}