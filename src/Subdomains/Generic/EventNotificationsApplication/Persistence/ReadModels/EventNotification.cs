using Application.Persistence.Common;
using Common;
using QueryAny;

namespace EventNotificationsApplication.Persistence.ReadModels;

[EntityName("EventNotification")]
public class EventNotification : ReadModelEntity
{
    public Optional<string> AggregateTypeFullName { get; set; }

    public Optional<string> EventJsonData { get; set; }

    public Optional<string> EventTypeFullName { get; set; }

    public Optional<string> StreamName { get; set; }

    public Optional<string> SubscriberRef { get; set; }

    public Optional<int> Version { get; set; }
}