using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Eventing.Common.Extensions;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Eventing.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class NotificationExtensionsSpec
{
    [Fact]
    public void WhenToDomainEventAndUnknownDomainEventType_ThenReturnsError()
    {
        var notification = new DomainEventNotification
        {
            EventJsonData = "adata",
            EventTypeFullName = "aneventtypefullname",
            AggregateTypeFullName = "anaggregatetypefullname",
            Id = "anid",
            StreamName = "astreamname",
            Version = 0
        };

        var result = notification.ToDomainEvent();

        result.Should().BeError(ErrorCode.Unexpected, Resources.EventingExtensions_ToDomainEvent_UnknownType.Format(
            "aneventtypefullname"));
    }

    [Fact]
    public void WhenToDomainEventAndFailsDeserialization_ThenReturnsError()
    {
        var notification = new DomainEventNotification
        {
            EventJsonData = "{}",
            EventTypeFullName = typeof(TestEvent).AssemblyQualifiedName!,
            AggregateTypeFullName = "anaggregatetypefullname",
            Id = "anid",
            StreamName = "astreamname",
            Version = 0
        };

        var result = notification.ToDomainEvent();

        result.Should().BeError(ErrorCode.Unexpected,
            Resources.EventingExtensions_ToDomainEvent_FailedToDeserialize.Format(
                typeof(TestEvent).AssemblyQualifiedName!,
                $"JSON deserialization for type '{typeof(TestEvent).FullName}' was missing required properties, including the following: {nameof(TestEvent.ARequiredProperty)}"));
    }

    [Fact]
    public void WhenToDomainEvent_ThenReturnsDomainEvent()
    {
        var originalEvent = new TestEvent
        {
            ARequiredProperty = "avalue",
            OccurredUtc = DateTime.UtcNow,
            RootId = "arootid"
        };
        var eventJson = originalEvent.ToEventJson();
        var notification = new DomainEventNotification
        {
            EventJsonData = eventJson,
            EventTypeFullName = typeof(TestEvent).AssemblyQualifiedName!,
            AggregateTypeFullName = "anaggregatetypefullname",
            Id = "anid",
            StreamName = "astreamname",
            Version = 0
        };

        var result = notification.ToDomainEvent();

        result.Should().BeSuccess();
        result.Value.Should().BeOfType<TestEvent>();
        result.Value.As<TestEvent>().RootId.Should().Be("arootid");
        result.Value.As<TestEvent>().ARequiredProperty.Should().Be("avalue");
    }
}

public class TestEvent : IDomainEvent
{
    public required string ARequiredProperty { get; set; }

    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; } = string.Empty;
}