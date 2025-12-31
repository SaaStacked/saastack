using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Common.UnitTests.Entities;

[Trait("Category", "Unit")]
public class DomainEventExtensionsSpec
{
    [Fact]
    public void WhenFromEventJsonWithEmptyJson_ThenReturnsInstance()
    {
        var result = "{}".FromEventJson(typeof(TestEvent));

        result.RootId.Should().BeNull();
        result.OccurredUtc.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenFromEventJsonWithPopulatedJson_ThenReturnsInstance()
    {
        var datum = DateTime.UtcNow;
        var result = new TestEvent
        {
            APropertyValue = "apropertyvalue",
            RootId = "anid",
            OccurredUtc = datum
        }.ToEventJson().FromEventJson(typeof(TestEvent));

        result.Should().BeOfType<TestEvent>();
        result.As<TestEvent>().APropertyValue.Should().Be("apropertyvalue");
        result.RootId.Should().Be("anid");
        result.OccurredUtc.Should().Be(datum);
    }

    [Fact]
    public void WhenToEventJsonWithPopulatedEvent_ThenReturnsJson()
    {
        var datum = DateTime.UtcNow;
        var result = new TestEvent
        {
            APropertyValue = "apropertyvalue",
            RootId = "anid",
            OccurredUtc = datum
        }.ToEventJson();

        result.Should()
            .Be(
                $"{{\"APropertyValue\":\"apropertyvalue\",\"OccurredUtc\":\"{datum.ToIso8601()}\",\"RootId\":\"anid\"}}");
    }

    [Fact]
    public void WhenToVersioned_ThenReturnsEvent()
    {
        var datum = DateTime.UtcNow.ToNearestSecond();
        var @event = new TestEvent
        {
            APropertyValue = "apropertyvalue",
            RootId = "anid",
            OccurredUtc = datum
        };
        var result = @event.ToVersioned("anid".ToIdentifierFactory(), typeof(TestEntity), 6).Value;

        result.Id.Should().Be("anid".ToId());
        result.LastPersistedAtUtc.Should().BeNone();
        result.OriginalEvent.Should().Be(@event);
        result.Version.Should().Be(6);
        result.AggregateType.Should().Be(typeof(TestEntity));
        result.EventType.Should().Be(typeof(TestEvent));
    }
}