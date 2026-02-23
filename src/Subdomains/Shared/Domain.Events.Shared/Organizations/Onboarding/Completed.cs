using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations.Onboarding;

public sealed class Completed : DomainEvent
{
    public Completed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Completed()
    {
    }

    public required string OrganizationId { get; set; }

    public required string CompletedBy { get; set; }
}