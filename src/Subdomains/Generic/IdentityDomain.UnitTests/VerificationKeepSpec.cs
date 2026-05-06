using Common;
using Common.Extensions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class VerificationKeepSpec
{
    private const string Token = "5n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";

    [Fact]
    public void WhenEmpty_ThenAssigned()
    {
        var verification = VerificationKeep.Empty;

        verification.IsVerifiable.Should().BeTrue();
        verification.IsVerified.Should().BeFalse();
        verification.IsVerifying.Should().BeFalse();
        verification.IsStillVerifying.Should().BeFalse();
        verification.Token.Should().BeNone();
        verification.ExpiresAt.Should().BeNone();
    }

    [Fact]
    public void WhenCreateWithInvalidToken_ThenReturnsError()
    {
        var verification = VerificationKeep.Create("aninvalidtoken", Optional<DateTime>.None, Optional<DateTime>.None);

        verification.Should().BeError(ErrorCode.Validation, Resources.VerificationKeep_InvalidToken);
    }

    [Fact]
    public void WhenRenewWithFutureExpiry_ThenStillVerifying()
    {
        var expiresAt = DateTime.UtcNow.Add(VerificationKeep.DefaultTokenExpiry).AddDays(1);
        var verification = VerificationKeep.Create(Token, Optional<DateTime>.None, Optional<DateTime>.None).Value;

        var result = verification.Renew(Token, expiresAt);

        result.Should().BeSuccess();
        result.Value.IsStillVerifying.Should().BeTrue();
        result.Value.Token.Should().Be(Token);
        result.Value.ExpiresAt.Should().BeSome(expiresAt);
    }

    [Fact]
    public void WhenRenewWithPastExpiry_ThenIsNotStillVerifying()
    {
        var expiresAt = DateTime.UtcNow.SubtractHours(1);
        var verification = VerificationKeep.Create(Token, Optional<DateTime>.None, Optional<DateTime>.None).Value;

        var result = verification.Renew(Token, expiresAt);

        result.Value.ExpiresAt.Should().BeSome(expiresAt);
        result.Value.IsStillVerifying.Should().BeFalse();
    }

    [Fact]
    public void WhenVerify_ThenIsNotStillVerifying()
    {
        var verification = VerificationKeep.Create(Token, Optional<DateTime>.None, Optional<DateTime>.None).Value;

        var result = verification.Verify();

        result.Should().BeSuccess();
        result.Value.IsStillVerifying.Should().BeFalse();
        result.Value.Token.Should().BeNone();
        result.Value.ExpiresAt.Should().BeNone();
        result.Value.VerifiedAt.Should().BeNear(DateTime.UtcNow.SubtractSeconds(1));
    }
}