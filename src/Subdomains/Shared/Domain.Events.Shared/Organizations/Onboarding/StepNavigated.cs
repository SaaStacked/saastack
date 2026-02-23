using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations.Onboarding;

public sealed class StepNavigated : DomainEvent
{
    public StepNavigated(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public StepNavigated()
    {
    }

    public required string FromStepId { get; set; }

    public required string NavigatedById { get; set; }

    public required string OrganizationId { get; set; }

    public required string ToStepId { get; set; }
}