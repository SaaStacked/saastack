using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.AuthTokens;

public sealed class TokensRefreshed : DomainEvent
{
    public TokensRefreshed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TokensRefreshed()
    {
    }

    public required string AccessToken { get; set; }

    public DateTime? AccessTokenExpiresOn { get; set; }

    public string? IdToken { get; set; }

    public DateTime? IdTokenExpiresOn { get; set; }

    public required string RefreshToken { get; set; }

    public required string RefreshTokenDigest { get; set; }

    public DateTime? RefreshTokenExpiresOn { get; set; }

    public required string UserId { get; set; }
}