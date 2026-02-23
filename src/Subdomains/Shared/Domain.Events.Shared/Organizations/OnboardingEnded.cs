using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class OnboardingEnded : DomainEvent
{
    public OnboardingEnded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public OnboardingEnded()
    {
    }

    public required string OnboardingId { get; set; }
}