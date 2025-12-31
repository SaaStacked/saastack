using Domain.Interfaces.Entities;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a changed event in an event stream
/// </summary>
public class EventStreamChangeEvent
{
    public required Type EventType { get; set; }

    public required string Id { get; set; }

    public DateTime? LastPersistedAtUtc { get; set; }

    public required IDomainEvent OriginalEvent { get; set; }

    public required Type RootAggregateType { get; set; }

    public required string StreamName { get; set; }

    public required int Version { get; set; }
}