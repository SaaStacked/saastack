using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Eventing.Interfaces.Projections;
using Infrastructure.Hosting.Common.ApplicationServices.Eventing.Projections;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using Moq;
using Xunit;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing.Projections;

[Trait("Category", "Unit")]
public class InProcessEventNotifyingStoreProjectionRelaySpec
{
    private readonly EventSourcingDddCommandStore<TestEventingAggregateRoot> _eventSourcingStore;
    private readonly TestProjection _projection;
    private readonly InProcessEventNotifyingStoreProjectionRelay _relay;

    public InProcessEventNotifyingStoreProjectionRelaySpec()
    {
        var recorder = new Mock<IRecorder>();
        var migrator = new Mock<IEventSourcedChangeEventMigrator>();
        migrator.Setup(m => m.Rehydrate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string id, string _, string _) => new TestEvent
            {
                Id = id
            });
        var checkpointStore = new Mock<IProjectionCheckpointRepository>();
        checkpointStore.Setup(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var domainFactory = new Mock<IDomainFactory>();
        var store = new Mock<IEventStore>();
        store.Setup(s => s.AddEventsAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<List<EventSourcedChangeEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("astreamname");
        _eventSourcingStore =
            new EventSourcingDddCommandStore<TestEventingAggregateRoot>(recorder.Object, domainFactory.Object,
                migrator.Object, store.Object);
        _projection = new TestProjection();
        var projections = new List<IReadModelProjection>
        {
            _projection
        };

        _relay = new InProcessEventNotifyingStoreProjectionRelay(recorder.Object, migrator.Object,
            checkpointStore.Object,
            projections, _eventSourcingStore);
    }

    [Fact]
    public void WhenStart_ThenStarted()
    {
        _relay.Start();

        _relay.IsStarted.Should().BeTrue();
    }

    [Fact]
    public async Task WhenEventHandlerFired_ThenProjectorProjects()
    {
        var aggregate = new TestEventingAggregateRoot("anid".ToId());
        aggregate.AddEvents(new TestEvent
        {
            Id = "aneventid1"
        }, new TestEvent
        {
            Id = "aneventid2"
        });
        _relay.Start();

        await _eventSourcingStore.SaveAsync(aggregate, CancellationToken.None);

        _projection.ProjectedEvents.Length.Should().Be(2);
        _projection.ProjectedEvents[0].As<TestEvent>().Id.Should().Be("aneventid1");
        _projection.ProjectedEvents[1].As<TestEvent>().Id.Should().Be("aneventid2");
    }
}