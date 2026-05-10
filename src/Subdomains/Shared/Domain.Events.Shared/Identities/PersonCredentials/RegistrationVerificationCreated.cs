using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.PersonCredentials;

public sealed class RegistrationVerificationCreated : DomainEvent
{
    public RegistrationVerificationCreated(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public RegistrationVerificationCreated()
    {
    }

    public required DateTime ExpiresAt { get; set; }

    public required string ResendToken { get; set; }

    public required string VerificationToken { get; set; }
}