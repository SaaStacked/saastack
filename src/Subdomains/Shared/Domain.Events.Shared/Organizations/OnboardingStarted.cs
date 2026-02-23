using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class OnboardingStarted : DomainEvent
{
    public OnboardingStarted(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public OnboardingStarted()
    {
    }

    public required string OnboardingId { get; set; }
}