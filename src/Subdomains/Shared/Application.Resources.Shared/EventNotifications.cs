using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class EventNotification : DomainEventNotification
{
    public required string SubscriberRef { get; set; }
}

public class DomainEventNotification : IIdentifiableResource
{
    public required string AggregateTypeFullName { get; set; }

    public required string EventJsonData { get; set; }

    public required string EventTypeFullName { get; set; }

    public DateTime? LastPersistedAtUtc { get; set; }

    public required string StreamName { get; set; }

    public required int Version { get; set; }

    public required string Id { get; set; }
}