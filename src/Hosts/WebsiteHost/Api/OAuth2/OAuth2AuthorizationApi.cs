using Application.Interfaces;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using Infrastructure.Web.Hosting.Common.Extensions;
using WebsiteHost.Application;

namespace WebsiteHost.Api.OAuth2;

[BaseApiFrom("/api")]
public class OAuth2AuthorizationApi : IWebApiService
{
    private readonly IOAuth2AuthorizationApplication _authorizationApplication;
    private readonly ICallerContextFactory _callerFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OAuth2AuthorizationApi(ICallerContextFactory callerFactory,
        IOAuth2AuthorizationApplication authorizationApplication,
        IHttpContextAccessor httpContextAccessor)
    {
        _callerFactory = callerFactory;
        _authorizationApplication = authorizationApplication;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    ///     Relays the authorization request to the OAuth2 authorization service in the backend API, and returns a redirect
    ///     URI in all cases.
    ///     This endpoint remembers the original request in a cookie, and relays it to the browser
    ///     <see cref="AuthenticationConstants.Cookies.PendingOAuth2Authorization" />, for automatic continuation after a login
    ///     flow, or a consent flow, during which, the authorization parameters will be lost to the XHR client.
    ///     Note: We will return the redirect in an HTTP-200 response instead of in an HTTP-302 response to allow the
    ///     XHR client to detect and perform the redirect in the browser properly, since the browser needs to redirect to both
    ///     a BEFFE login/consent page, or to an external application redirectUri (which cannot be done properly by the XHR
    ///     client itself, and must be done by the browser).
    /// </summary>
    public async Task<ApiPostResult<string, AuthorizeOAuth2Response>> Authorize(
        AuthorizeOAuth2Request request, CancellationToken cancellationToken)
    {
        var finalRequest = request;
        if (IsAuthorizationAttemptInProgress(out var pendingRequest))
        {
            MergeWithInProgressAuthorization(finalRequest, pendingRequest);
        }

        var authorized = await _authorizationApplication.AuthorizeAsync(_callerFactory.Create(), finalRequest.ClientId!,
            finalRequest.RedirectUri!, finalRequest.ResponseType!, finalRequest.Scope!, finalRequest.State,
            finalRequest.Nonce,
            finalRequest.CodeChallengeMethod, finalRequest.CodeChallenge, cancellationToken);
        if (authorized.IsFailure)
        {
            WipePreviousAuthorizationAttempt();
            return () => authorized.Error;
        }

        var location = new Uri(authorized.Value);
        if (location.AbsolutePath.Contains(WebsiteUiService.LoginPageRoute))
        {
            RememberInProgressAuthorizationAttempt(request);
        }
        else
        {
            WipePreviousAuthorizationAttempt();
        }

        var isExternal = location.Host != _httpContextAccessor.HttpContext!.Request.Host.Host;
        return () => new PostResult<AuthorizeOAuth2Response>(new AuthorizeOAuth2Response
        {
            Redirect = new AuthorizeRedirect
            {
                RedirectUri = location.AbsoluteUri,
                IsLogin = !isExternal && location.AbsolutePath.Contains(WebsiteUiService.LoginPageRoute),
                IsConsent = !isExternal && location.AbsolutePath.Contains(WebsiteUiService.OAuth2ConsentPageRoute),
                IsExternal = isExternal
            }
        });
    }

    /// <summary>
    ///     Relays the consent request to the OAuth2 authorization service in the backend API, and returns either:
    ///     1. A redirect to an OAuth2 error. Not a HTTP-302 response.
    ///     2. Confirmation of consent
    ///     This endpoint relays the original authorization request in a cookie to the browser
    ///     <see cref="AuthenticationConstants.Cookies.PendingOAuth2Authorization" />, for automatic continuation after
    ///     this consent flow, during which, the original authorization parameters will be lost to the XHR client.
    ///     Note: We will return the redirect in an HTTP-200 response instead of in an HTTP-302 response to allow the
    ///     XHR client to detect and perform the redirect in the browser properly, since the browser needs to redirect to
    ///     an external application redirectUri (which cannot be done properly by the XHR client itself,
    ///     and must be done by the browser).
    /// </summary>
    public async Task<ApiPostResult<ClientConsentResult, ConsentOAuth2ClientResponse>> ConsentToClient(
        ConsentOAuth2ClientRequest request, CancellationToken cancellationToken)
    {
        var consented = await _authorizationApplication.ConsentToClientAsync(_callerFactory.Create(), request.Id!,
            request.RedirectUri!, request.Scope!, request.Consented, request.State, cancellationToken);
        if (consented.IsFailure)
        {
            return () => consented.Error;
        }

        var result = consented.Value;
        if (result.DenyErrorRedirectUri.Exists())
        {
            WipePreviousAuthorizationAttempt();
            return () => new PostResult<ConsentOAuth2ClientResponse>(new ConsentOAuth2ClientResponse
            {
                Redirect = new ConsentRedirect
                {
                    RedirectUri = new Uri(result.DenyErrorRedirectUri!).AbsoluteUri,
                    IsConsented = false
                }
            });
        }

        RelayInProgressAuthorizationAttempt();
        return () => new PostResult<ConsentOAuth2ClientResponse>(new ConsentOAuth2ClientResponse
        {
            Redirect = new ConsentRedirect
            {
                RedirectUri = null,
                IsConsented = true
            }
        });
    }

    private static void MergeWithInProgressAuthorization(AuthorizeOAuth2Request actual,
        AuthorizeOAuth2Request? pendingRequest)
    {
        if (pendingRequest.NotExists())
        {
            return;
        }

        // Required fields
        actual.ClientId = actual.ClientId ?? pendingRequest.ClientId;
        actual.RedirectUri = actual.RedirectUri ?? pendingRequest.RedirectUri;
        actual.ResponseType = actual.ResponseType ?? pendingRequest.ResponseType;
        actual.Scope = actual.Scope ?? pendingRequest.Scope;

        // Optional fields
        actual.State = actual.State ?? pendingRequest.State;
        actual.Nonce = actual.Nonce ?? pendingRequest.Nonce;
        actual.CodeChallenge = actual.CodeChallenge ?? pendingRequest.CodeChallenge;
        actual.CodeChallengeMethod = actual.CodeChallengeMethod ?? pendingRequest.CodeChallengeMethod;
    }

    private void RelayInProgressAuthorizationAttempt()
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        var request = httpContext.Request;
        var response = httpContext.Response;

        if (request.Cookies.TryGetValue(AuthenticationConstants.Cookies.PendingOAuth2Authorization,
                out var value))
        {
            response.Cookies.Append(AuthenticationConstants.Cookies.PendingOAuth2Authorization, value);
        }
    }

    private void WipePreviousAuthorizationAttempt()
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        var response = httpContext.Response;

        response.Cookies.Delete(AuthenticationConstants.Cookies.PendingOAuth2Authorization);
    }

    private bool IsAuthorizationAttemptInProgress(out AuthorizeOAuth2Request? requestDto)
    {
        requestDto = null;
        var httpContext = _httpContextAccessor.HttpContext!;
        var request = httpContext.Request;

        if (request.Cookies.TryGetValue(AuthenticationConstants.Cookies.PendingOAuth2Authorization,
                out var value))
        {
            requestDto = value.FromJson<AuthorizeOAuth2Request>()!;
            return true;
        }

        return false;
    }

    private void RememberInProgressAuthorizationAttempt(AuthorizeOAuth2Request requestDto)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        var response = httpContext.Response;
        var oAuth2Parameters = requestDto.ToJson(false)!;
        var time = DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultPendingOAuth2AuthorizationExpiry);

        response.Cookies.Append(AuthenticationConstants.Cookies.PendingOAuth2Authorization,
            oAuth2Parameters, ((DateTime?)time).GetCookieOptions());
    }
}