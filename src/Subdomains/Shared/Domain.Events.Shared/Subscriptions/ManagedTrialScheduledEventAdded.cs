using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Subscriptions;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class ManagedTrialScheduledEventAdded : DomainEvent
{
    public ManagedTrialScheduledEventAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ManagedTrialScheduledEventAdded()
    {
    }

    public TrialScheduledEventAction EventAction { get; set; }

    public TrialScheduledEventTrack EventAppliesWhen { get; set; }

    public required string EventId { get; set; }

    public required Dictionary<string, string> EventMetadata { get; set; }

    public required string OwningEntityId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }
}