#if TESTINGONLY
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.External.TestingOnly.ApplicationServices;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;

namespace TestingStubApiHost.Api;

/// <summary>
///     Represents an example of a testing stub that stands in for a fake SSO provider,
///     that is used by the WebSiteHost Javascript App.
///     In production builds, this host is not deployed.
///     This API mimics what the real fake provider does, with some pre-programmed responses.
/// </summary>
[BaseApiFrom("/fakessoprovider")]
public class StubFakeSsoProviderApi : StubApiBase
{
    private readonly Dictionary<string, TokenContext> _tokenContexts = new();

    public StubFakeSsoProviderApi(IRecorder recorder, IConfigurationSettings settings) : base(recorder, settings)
    {
    }

    public async Task<ApiRedirectResult<string, GenericOAuth2GrantAuthorizationResponse>> Authorize(
        GenericOAuth2GrantAuthorizationRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        if (request.GrantType == OAuth2Constants.GrantTypes.AuthorizationCode)
        {
            Recorder.TraceInformation(null,
                "StubFakeSsoProviderApi: Token Exchange grant {GrantType}, with {Code} or RefreshToken {RefreshToken}, for scope {Scope}, with credentials {ClientId} and {ClientSecret}, and redirect to {RedirectUri}, with PKCE: verifier {CodeVerifier}, and state: {State}",
                request.GrantType ?? "none", request.Code ?? "none", request.RefreshToken ?? "none",
                request.Scope ?? "none", request.ClientId ?? "none", request.ClientSecret ?? "none",
                request.RedirectUri ?? "none", request.CodeVerifier ?? "none", request.State ?? "none");
        }
        else
        {
            //Assume it's an authorization grant
            Recorder.TraceInformation(null,
                "StubFakeSsoProviderApi: Authorize grant {Type}, for scope {Scope}, for client {ClientId}, and redirect to {RedirectUri}, with PKCE: challenge: {CodeChallenge} ({CodeChallengeMethod}), and state: {State}",
                request.ResponseType ?? "none", request.Scope ?? "none", request.ClientId ?? "none",
                request.RedirectUri ?? "none", request.CodeChallenge ?? "none", request.CodeChallengeMethod ?? "none",
                request.State ?? "none");
        }

        if (request.GrantType == OAuth2Constants.GrantTypes.AuthorizationCode)
        {
            if (request.Code.HasNoValue())
            {
                return () => Error.NotAuthenticated();
            }

            var (accessToken, refreshToken) = GenerateTokens();
            _tokenContexts.Add(refreshToken, new TokenContext(accessToken, request.Code));

            return () =>
                new RedirectResult<GenericOAuth2GrantAuthorizationResponse>(new GenericOAuth2GrantAuthorizationResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = (int)AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalSeconds,
                    Scope = request.Scope,
                    IdToken = null,
                    TokenType = OAuth2Constants.TokenTypes.Bearer
                });
        }

        if (request.GrantType == OAuth2Constants.GrantTypes.RefreshToken)
        {
            if (request.RefreshToken.HasNoValue())
            {
                return () => Error.NotAuthenticated();
            }

            if (!_tokenContexts.TryGetValue(request.RefreshToken, out var tokenContext))
            {
                return () => Error.NotAuthenticated();
            }

            var (accessToken, refreshToken) = GenerateTokens();
            _tokenContexts.Add(refreshToken, new TokenContext(accessToken, tokenContext.Code));

            return () =>
                new RedirectResult<GenericOAuth2GrantAuthorizationResponse>(new GenericOAuth2GrantAuthorizationResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = (int)AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalSeconds,
                    Scope = request.Scope,
                    IdToken = null,
                    TokenType = OAuth2Constants.TokenTypes.Bearer
                });
        }

        if (request.ResponseType == OAuth2Constants.ResponseTypes.Code)
        {
            var redirectUri = $"{request.RedirectUri}?code={FakeOAuth2Service.AuthCode1}&state={request.State}";
            return () => new RedirectResult<GenericOAuth2GrantAuthorizationResponse>(
                new GenericOAuth2GrantAuthorizationResponse(), redirectUri);
        }

        return () => Error.NotAuthenticated();
    }

    private static (string accessToken, string refreshToken) GenerateTokens()
    {
        var expiresOn = DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims:
            [
                new Claim(ClaimTypes.GivenName, "agivenname"),
                new Claim(ClaimTypes.Surname, "asurname"),
                new Claim(AuthenticationConstants.Claims.ForTimezone, Timezones.Default.ToString()),
                new Claim(ClaimTypes.Country, CountryCodes.Default.ToString())
            ], expires: expiresOn,
            issuer: "FakeSSOProvider"
        ));
        var refreshToken = Guid.NewGuid().ToString();

        return (accessToken, refreshToken);
    }

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record TokenContext(string Token, string Code);
}
#endif