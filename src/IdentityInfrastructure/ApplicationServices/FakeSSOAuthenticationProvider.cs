#if TESTINGONLY
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using IdentityApplication.ApplicationServices;

namespace IdentityInfrastructure.ApplicationServices;

/// <summary>
///     Provides a fake example <see cref="ISSOAuthenticationProvider" /> that can be copied for real providers,
///     such as Google, Microsoft, Facebook and others
/// </summary>
public class FakeSSOAuthenticationProvider : ISSOAuthenticationProvider
{
    public const string SSOName = "testsso";
    private const string ServiceName = "FakeOAuth2Service";
    private readonly IOAuth2Service _auth2Service;

    public FakeSSOAuthenticationProvider() : this(new FakeOAuth2Service())
    {
    }

    private FakeSSOAuthenticationProvider(IOAuth2Service auth2Service)
    {
        _auth2Service = auth2Service;
    }

    public async Task<Result<SSOAuthUserInfo, Error>> AuthenticateAsync(ICallerContext caller, string authCode,
        string? codeVerifier, string? emailAddress, CancellationToken cancellationToken)
    {
        authCode.ThrowIfNotValuedParameter(nameof(authCode),
            Resources.AnySSOAuthenticationProvider_MissingAuthCode);

        if (emailAddress.HasNoValue())
        {
            return Error.RuleViolation(Resources.FakeSSOAuthenticationProvider_MissingUsername);
        }

        var retrievedTokens =
            await _auth2Service.ExchangeCodeForTokensAsync(caller,
                new OAuth2CodeTokenExchangeOptions(ServiceName, authCode, codeVerifier, emailAddress),
                cancellationToken);
        if (retrievedTokens.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var tokens = retrievedTokens.Value;

        return FakeOAuth2Service.GetUserInfoFromTokens(authCode, tokens);
    }

    public string ProviderName => SSOName;

    public async Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller,
        string refreshToken, CancellationToken cancellationToken)
    {
        refreshToken.ThrowIfNotValuedParameter(nameof(refreshToken),
            Resources.AnySSOAuthenticationProvider_MissingRefreshToken);

        var retrievedTokens =
            await _auth2Service.RefreshTokenAsync(caller,
                new OAuth2RefreshTokenOptions(ServiceName, refreshToken),
                cancellationToken);
        if (retrievedTokens.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var tokens = retrievedTokens.Value;
        return FakeOAuth2Service.GetProviderTokensFromTokens(SSOName, tokens);
    }
}
#endif