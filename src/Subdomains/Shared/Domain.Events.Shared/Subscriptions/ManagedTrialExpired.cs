using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class ManagedTrialExpired : DomainEvent
{
    public ManagedTrialExpired(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ManagedTrialExpired()
    {
    }

    public required string OwningEntityId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

    public required DateTime TrialExpiresAt { get; set; }

    public required DateTime TrialStartedAt { get; set; }

    public required int TrialDurationDays { get; set; }
}