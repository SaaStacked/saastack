using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.UserProfiles;

public sealed class AttributeAdded : DomainEvent
{
    public AttributeAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public AttributeAdded()
    {
    }

    public required string Name { get; set; }

    public required string UserId { get; set; }

    public required string Value { get; set; }
}