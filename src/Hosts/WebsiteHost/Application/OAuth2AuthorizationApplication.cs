using System.Net;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Extensions;

namespace WebsiteHost.Application;

public class OAuth2AuthorizationApplication : IOAuth2AuthorizationApplication
{
    private readonly IServiceClient _serviceClient;

    public OAuth2AuthorizationApplication(IServiceClient serviceClient)
    {
        _serviceClient = serviceClient;
    }

    public async Task<Result<string, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri, OAuth2ResponseType? responseType,
        string scope, string? state, string? nonce, OpenIdConnectCodeChallengeMethod? codeChallengeMethod,
        string? codeChallenge, CancellationToken cancellationToken)
    {
        var location = string.Empty;
        var authorized = await _serviceClient.PostAsync(caller, new AuthorizeOAuth2Request
            {
                ClientId = clientId,
                RedirectUri = redirectUri,
                ResponseType = responseType,
                Scope = scope,
                State = state,
                Nonce = nonce,
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = codeChallengeMethod
            }, req =>
            {
                if (caller is
                    {
                        IsAuthenticated: true,
                        Authorization: { HasValue: true, Value.Method: ICallerContext.AuthorizationMethod.AuthNCookie }
                    })
                {
                    var token = caller.Authorization.Value.Value.Value;
                    req.SetJWTBearerToken(token);
                }
                else
                {
                    req.RemoveAuthorization();
                }
            },
            res =>
            {
                if (res.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect)
                {
                    location = res.Headers.Location!.AbsoluteUri;
                }
            }, cancellationToken);

        if (authorized.IsFailure)
        {
            return authorized.Error.ToError(); // Should never get here
        }

        return location;
    }

    public async Task<Result<ClientConsentResult, Error>> ConsentToClientAsync(ICallerContext caller, string clientId,
        string redirectUri, string scope, bool consent,
        string? state, CancellationToken cancellationToken)
    {
        string? location = null;
        var consented = await _serviceClient.PostAsync(caller, new ConsentOAuth2ClientForCallerRequest
            {
                Id = clientId,
                RedirectUri = redirectUri,
                Scope = scope,
                Consented = consent,
                State = state
            }, req =>
            {
                if (caller is
                    {
                        IsAuthenticated: true,
                        Authorization: { HasValue: true, Value.Method: ICallerContext.AuthorizationMethod.AuthNCookie }
                    })
                {
                    var token = caller.Authorization.Value.Value.Value;
                    req.SetJWTBearerToken(token);
                }
                else
                {
                    req.RemoveAuthorization();
                }
            },
            res =>
            {
                if (res.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect)
                {
                    location = res.Headers.Location!.AbsoluteUri;
                }
            }, cancellationToken);

        if (consented.IsFailure)
        {
            return consented.Error.ToError(); // Should never get here
        }

        return new ClientConsentResult
        {
            DenyErrorRedirectUri = location,
            Consent = consented.Value.Consent
        };
    }
}