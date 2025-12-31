using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Eventing.Common.Projections;
using Infrastructure.Eventing.Interfaces.Projections;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Eventing.Common.UnitTests.Projections;

[Trait("Category", "Unit")]
public sealed class InProcessSynchronousReadModelProjectorSpec : IDisposable
{
    private readonly Mock<IProjectionCheckpointRepository> _checkpointRepository;
    private readonly Mock<IReadModelProjection> _projection;
    private readonly InProcessSynchronousReadModelProjector _projector;

    public InProcessSynchronousReadModelProjectorSpec()
    {
        var recorder = new Mock<IRecorder>();
        _checkpointRepository = new Mock<IProjectionCheckpointRepository>();
        _projection = new Mock<IReadModelProjection>();
        _projection.Setup(prj => prj.RootAggregateType)
            .Returns(typeof(TestAggregateRoot));
        _projection.Setup(prj => prj.ProjectEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var projections = new List<IReadModelProjection> { _projection.Object };
        _projector =
            new InProcessSynchronousReadModelProjector(recorder.Object, _checkpointRepository.Object,
                projections.ToArray());
    }

    ~InProcessSynchronousReadModelProjectorSpec()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _projector.Dispose();
        }
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndNoEvents_ThenReturns()
    {
        await _projector.WriteEventStreamAsync("astreamname", [],
            CancellationToken.None);

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _projection.Verify(prj => prj.ProjectEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkpointRepository.Verify(
            cs => cs.SaveCheckpointAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndNoConfiguredProjection_ThenReturnsError()
    {
        _projection.Setup(prj => prj.RootAggregateType)
            .Returns(typeof(string));

        var result = await _projector.WriteEventStreamAsync("astreamname", [
            new EventStreamChangeEvent
            {
                OriginalEvent = new TestEvent { RootId = "aneventid1" },
                RootAggregateType = typeof(TestAggregateRoot),
                EventType = typeof(TestEvent),
                Id = null!,
                LastPersistedAtUtc = null,
                StreamName = null!,
                Version = 0
            }
        ], CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.ReadModelProjector_ProjectionNotConfigured.Format(typeof(TestAggregateRoot)));

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _projection.Verify(prj => prj.ProjectEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkpointRepository.Verify(
            cs => cs.SaveCheckpointAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndEventVersionGreaterThanCheckpoint_ThenThrows()
    {
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        await _projector.Invoking(x => x.WriteEventStreamAsync("astreamname", [
                new EventStreamChangeEvent
                {
                    OriginalEvent = new TestEvent { RootId = "aneventid1" },
                    RootAggregateType = typeof(TestAggregateRoot),
                    EventType = typeof(TestEvent),
                    Id = null!,
                    LastPersistedAtUtc = null,
                    StreamName = null!,
                    Version = 6
                }
            ], CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessageLike(Resources.ReadModelProjector_CheckpointError.Format("astreamname", 5, 6));
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndEventVersionLessThanCheckpoint_ThenSkipsPreviousVersions()
    {
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        await _projector.WriteEventStreamAsync("astreamname", [
            new EventStreamChangeEvent
            {
                Id = "anid1",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid1" },
                Version = 4,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            },

            new EventStreamChangeEvent
            {
                Id = "anid2",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid2" },
                Version = 5,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            },

            new EventStreamChangeEvent
            {
                Id = "anid3",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid3" },
                Version = 6,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            }
        ], CancellationToken.None);

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()), Times.Never);
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
        _checkpointRepository.Verify(cs => cs.SaveCheckpointAsync("astreamname", 7, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndFirstEverEvent_ThenProjectsEvents()
    {
        const int startingCheckpoint = ProjectionCheckpointRepository.StartingCheckpointVersion;
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .ReturnsAsync(startingCheckpoint);

        await _projector.WriteEventStreamAsync("astreamname", [
            new EventStreamChangeEvent
            {
                Id = "anid1",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid1" },
                Version = startingCheckpoint,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            },

            new EventStreamChangeEvent
            {
                Id = "anid2",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid2" },
                Version = startingCheckpoint + 1,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            },

            new EventStreamChangeEvent
            {
                Id = "anid3",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid3" },
                Version = startingCheckpoint + 2,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            }
        ], CancellationToken.None);

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
        _checkpointRepository.Verify(cs =>
            cs.SaveCheckpointAsync("astreamname", startingCheckpoint + 3, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndEventNotHandledByProjection_ThenReturnsError()
    {
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _projection.Setup(prj => prj.ProjectEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _projector.WriteEventStreamAsync("astreamname", [
            new EventStreamChangeEvent
            {
                Id = "anid1",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid1" },
                Version = 3,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            }
        ], CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected,
            Resources.ReadModelProjector_ProjectionError_MissingHandler.Format("IReadModelProjectionProxy", "anid1",
                typeof(TestEvent).AssemblyQualifiedName!));
        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _checkpointRepository.Verify(
            cs => cs.SaveCheckpointAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAsync_ThenProjectsEvents()
    {
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        await _projector.WriteEventStreamAsync("astreamname", [
            new EventStreamChangeEvent
            {
                Id = "anid1",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid1" },
                Version = 3,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            },

            new EventStreamChangeEvent
            {
                Id = "anid2",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid2" },
                Version = 4,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            },

            new EventStreamChangeEvent
            {
                Id = "anid3",
                RootAggregateType = typeof(TestAggregateRoot),
                OriginalEvent = new TestEvent { RootId = "aneventid3" },
                Version = 5,
                EventType = typeof(TestEvent),
                LastPersistedAtUtc = null,
                StreamName = null!
            }
        ], CancellationToken.None);

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
        _checkpointRepository.Verify(cs => cs.SaveCheckpointAsync("astreamname", 6, It.IsAny<CancellationToken>()));
    }
}