using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.EndUsers;

public sealed class DefaultMembershipChanged : DomainEvent
{
    public DefaultMembershipChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public DefaultMembershipChanged()
    {
    }

    public required List<string> Features { get; set; }

    public string? FromMembershipId { get; set; }

    public required List<string> Roles { get; set; }

    public required string ToMembershipId { get; set; }

    public required string ToOrganizationId { get; set; }
}