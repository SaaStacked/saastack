using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OAuth2.Clients;

public sealed class LogoAdded : DomainEvent
{
    public LogoAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public LogoAdded()
    {
    }

    public required string LogoId { get; set; }

    public required string LogoUrl { get; set; }
}