using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class VerificationKeep : ValueObjectBase<VerificationKeep>
{
    public static readonly TimeSpan DefaultTokenExpiry = TimeSpan.FromDays(1);
    public static readonly VerificationKeep Empty = new(Optional<string>.None, Optional<DateTime>.None,
        Optional<DateTime>.None);

    public static Result<VerificationKeep, Error> Create(Optional<string> token, Optional<DateTime> expiresAt,
        Optional<DateTime> verifiedAt)
    {
        if (token.HasValue)
        {
            if (token.Value.IsInvalidParameter(Validations.Credentials.Password.VerificationToken, nameof(token),
                    Resources.VerificationKeep_InvalidToken, out var error1))
            {
                return error1;
            }
        }

        return new VerificationKeep(token, expiresAt, verifiedAt);
    }

    private VerificationKeep(Optional<string> token, Optional<DateTime> expiresAt, Optional<DateTime> verifiedAt)
    {
        Token = token;
        ExpiresAt = expiresAt;
        VerifiedAt = verifiedAt;
    }

    public Optional<DateTime> ExpiresAt { get; }

    public bool IsStillVerifying => IsVerifying && ExpiresAt > DateTime.UtcNow;

    public bool IsVerifiable => !Token.HasValue && !ExpiresAt.HasValue && !VerifiedAt.HasValue;

    public bool IsVerified => !Token.HasValue && !ExpiresAt.HasValue && VerifiedAt.HasValue;

    public bool IsVerifying => Token.HasValue && ExpiresAt.HasValue;

    public Optional<string> Token { get; }

    public Optional<DateTime> VerifiedAt { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<VerificationKeep> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new VerificationKeep(
                parts[0],
                parts[1].ToOptional(val => val.FromIso8601()),
                parts[2].ToOptional(val => val.FromIso8601()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Token, ExpiresAt, VerifiedAt];
    }

    public Result<VerificationKeep, Error> Renew(string token, DateTime expiresAt)
    {
        if (token.IsInvalidParameter(Validations.Credentials.Password.VerificationToken, nameof(token),
                Resources.VerificationKeep_InvalidToken, out var error))
        {
            return error;
        }

        return new VerificationKeep(token, expiresAt, Optional<DateTime>.None);
    }

#if TESTINGONLY
    public VerificationKeep TestingOnly_ExpireToken()
    {
        return new VerificationKeep(Token, DateTime.UtcNow, Optional<DateTime>.None);
    }
#endif

    public Result<VerificationKeep, Error> Verify()
    {
        return new VerificationKeep(Optional<string>.None, Optional<DateTime>.None, DateTime.UtcNow.SubtractSeconds(1));
    }
}