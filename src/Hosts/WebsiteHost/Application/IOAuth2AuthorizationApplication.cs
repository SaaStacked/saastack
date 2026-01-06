using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace WebsiteHost.Application;

public interface IOAuth2AuthorizationApplication
{
    Task<Result<string, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri, OAuth2ResponseType? responseType, string scope, string? state, string? nonce,
        OpenIdConnectCodeChallengeMethod? codeChallengeMethod, string? codeChallenge,
        CancellationToken cancellationToken);

    Task<Result<ClientConsentResult, Error>> ConsentToClientAsync(ICallerContext caller, string clientId,
        string redirectUri,
        string scope, bool consent, string? state, CancellationToken cancellationToken);
}

public class ClientConsentResult
{
    public OAuth2ClientConsent? Consent { get; set; }

    public string? DenyErrorRedirectUri { get; set; }
}