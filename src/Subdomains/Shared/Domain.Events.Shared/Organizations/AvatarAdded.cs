using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class AvatarAdded : DomainEvent
{
    public AvatarAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public AvatarAdded()
    {
    }

    public required string AvatarId { get; set; }

    public required string AvatarUrl { get; set; }
}