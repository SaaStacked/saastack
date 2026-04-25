using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Subscriptions;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class ManagedTrialEventScheduleEnded : DomainEvent
{
    public ManagedTrialEventScheduleEnded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ManagedTrialEventScheduleEnded()
    {
    }

    public required string OwningEntityId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

    public required TrialScheduledEndingReason Reason { get; set; }
}