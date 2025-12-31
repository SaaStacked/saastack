using Application.Persistence.Interfaces;
using Application.Persistence.Shared.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Persistence.Shared.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class MessageBusExtensionsSpec
{
    [Fact]
    public void WhenToDomainEventingMessage_ThenReturnsMessage()
    {
        var changeEvent = new EventStreamChangeEvent
        {
            Id = "anid",
            LastPersistedAtUtc = DateTime.UtcNow,
            OriginalEvent = new TestDomainEvent(),
            EventType = typeof(TestDomainEvent),
            RootAggregateType = typeof(TestAggregateRoot),
            StreamName = "astreamname",
            Version = 9
        };

        var result = changeEvent.ToDomainEventingMessage();

        result.Event!.Id.Should().Be("anid");
        result.Event.LastPersistedAtUtc.Should().Be(changeEvent.LastPersistedAtUtc);
        result.Event.EventJsonData.Should().Be(changeEvent.OriginalEvent.ToEventJson());
        result.Event.EventTypeFullName.Should().Be(typeof(TestDomainEvent).AssemblyQualifiedName!);
        result.Event.AggregateTypeFullName.Should().Be(typeof(TestAggregateRoot).AssemblyQualifiedName);
        result.Event.StreamName.Should().Be("astreamname");
        result.Event.Version.Should().Be(9);
    }
}

public class TestDomainEvent : IDomainEvent
{
    public string? AProperty { get; set; }

    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; } = "arootid";
}

public class TestAggregateRoot
{
}