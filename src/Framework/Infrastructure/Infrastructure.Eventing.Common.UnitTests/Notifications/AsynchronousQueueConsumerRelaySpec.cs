using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Infrastructure.Eventing.Common.Notifications;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Eventing.Common.UnitTests.Notifications;

[Trait("Category", "Unit")]
public class AsynchronousQueueConsumerRelaySpec
{
    private readonly Mock<IDomainEventingMessageBusTopic> _queue;
    private readonly AsynchronousQueueConsumerRelay _relay;

    public AsynchronousQueueConsumerRelaySpec()
    {
        _queue = new Mock<IDomainEventingMessageBusTopic>();
        var hostRegionService = new Mock<IHostSettings>();
        hostRegionService.Setup(c => c.GetRegion())
            .Returns(DatacenterLocations.Local);

        _relay = new AsynchronousQueueConsumerRelay(_queue.Object, hostRegionService.Object);
    }

    [Fact]
    public async Task WhenRelayAsyncAndQueueReturnsError_ThenReturnsError()
    {
        _queue.Setup(c =>
                c.SendAsync(It.IsAny<ICallContext>(), It.IsAny<DomainEventingMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.RuleViolation("amessage"));
        var changeEvent = new EventStreamChangeEvent
        {
            OriginalEvent = new TestDomainEvent(),
            RootAggregateType = typeof(TestAggregateRoot),
            EventType = typeof(TestDomainEvent),
            Id = "anid",
            LastPersistedAtUtc = DateTime.UtcNow,
            StreamName = "astreamname",
            Version = 9
        };

        var result =
            await _relay.RelayDomainEventAsync(changeEvent, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.AsynchronousConsumerRelay_RelayFailed.Format(
                "AsynchronousQueueConsumerRelay",
                "arootid", typeof(TestDomainEvent).AssemblyQualifiedName!)).ToString());
        _queue.Verify(c => c.SendAsync(It.Is<ICallContext>(call =>
            call.HostRegion == DatacenterLocations.Local), It.Is<DomainEventingMessage>(msg =>
            msg.Event!.Id == changeEvent.Id
            && msg.Event.LastPersistedAtUtc == changeEvent.LastPersistedAtUtc
            && msg.Event.EventTypeFullName == changeEvent.EventType.AssemblyQualifiedName
            && msg.Event.AggregateTypeFullName == changeEvent.RootAggregateType.AssemblyQualifiedName
            && msg.Event.StreamName == changeEvent.StreamName
            && msg.Event.Version == changeEvent.Version
            && msg.Event.EventJsonData == changeEvent.OriginalEvent.ToEventJson()
        ), It.IsAny<CancellationToken>()));
    }
}