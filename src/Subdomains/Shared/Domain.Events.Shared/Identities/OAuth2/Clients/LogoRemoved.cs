using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.OAuth2.Clients;

public sealed class LogoRemoved : DomainEvent
{
    public LogoRemoved(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public LogoRemoved()
    {
    }

    public required string LogoId { get; set; }
}