using Application.Persistence.Common;
using Common;
using QueryAny;

namespace Infrastructure.Persistence.Common.ApplicationServices;

/// <summary>
///     Provides an entity for storing in an Event Store
/// </summary>
[EntityName("EventStore")]
public class EventStoreEntity : ReadModelEntity
{
    public Optional<string> AggregateTypeFullName { get; set; }

    public Optional<string> EventJsonData { get; set; }

    public Optional<string> EventTypeFullName { get; set; }

    public Optional<string> StreamName { get; set; }

    public int Version { get; set; }
}