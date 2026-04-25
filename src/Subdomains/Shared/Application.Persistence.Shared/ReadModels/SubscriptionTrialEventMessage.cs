using Application.Interfaces;
using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName(WorkerConstants.Queues.SubscriptionTrialEvents)]
public class SubscriptionTrialEventMessage : QueuedMessage
{
    public QueuedTrialEvent? Event { get; set; }

    public string? OwningEntityId { get; set; }

    public string? ProviderName { get; set; }

    public QueuedTrialSignal? Signal { get; set; }
}

public class QueuedTrialSignal
{
    public required string SignalId { get; set; }
}

public class QueuedTrialEvent
{
    public required int DelayInDays { get; set; }

    public required string Action { get; set; }

    public required string Track { get; set; }

    public required string EventId { get; set; }

    public required Dictionary<string, string> Metadata { get; set; }
}