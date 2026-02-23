using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations.Onboarding;

public sealed class StepStateChanged : DomainEvent
{
    public StepStateChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public StepStateChanged()
    {
    }

    public required string CurrentStepId { get; set; }

    public required Dictionary<string, string> Values { get; set; }

    public required string OrganizationId { get; set; }
}