using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Resources.Shared;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.Website.Common;
using UnitTesting.Common;
using Xunit;
using AuthorizeOAuth2Request = Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.AuthorizeOAuth2Request;

namespace WebsiteHost.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("WEBSITE")]
public class OAuth2AuthApiZSpec : WebsiteSpec<Program, ApiHost1.Program>
{
    private readonly JsonSerializerOptions _jsonOptions;

    public OAuth2AuthApiZSpec(WebApiSetup<Program> setup) : base(setup)
    {
        _jsonOptions = setup.GetRequiredService<JsonSerializerOptions>();
    }

    [Fact]
    public async Task WhenAuthorizeAndNoContinuationAndEmptyRequest_ThenReturnsError()
    {
        var request = new AuthorizeOAuth2Request();

        var result = await HttpApi.PostAsync(request.MakeApiRoute(),
            JsonContent.Create(request, options: _jsonOptions),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Headers.Location.Should().BeNull();
        result.GetCookie(CookieType.PendingOAuth2Authorization).Should().BeNone();
    }

    [Fact]
    public async Task WhenAuthorizeAndUnauthenticated_ThenReturnsRedirectToLoginAndSavesContinuation()
    {
        var client = await CreateClientAsync();
        var request = new AuthorizeOAuth2Request
        {
            ClientId = client.Id,
            RedirectUri = "https://externalhost/callback",
            ResponseType = OAuth2ResponseType.Code,
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
        };

        var result = await HttpApi.PostAsync(request.MakeApiRoute(),
            JsonContent.Create(request, options: _jsonOptions),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = result.Content.ReadFromJsonAsync<AuthorizeOAuth2Response>(JsonOptions).Result!;
        response.Redirect.IsLogin.Should().BeTrue();
        response.Redirect.IsConsent.Should().BeFalse();
        response.Redirect.IsExternal.Should().BeFalse();
        response.Redirect.RedirectUri.Should().Contain(WebsiteUiService.LoginPageRoute);
        result.GetCookie(CookieType.PendingOAuth2Authorization).Should().NotBeNone();
    }

    [Fact]
    public async Task WhenAuthorizeAfterAuthenticationWithContinuation_ThenReturnsRedirectToConsent()
    {
        var client = await CreateClientAsync();
        var authorizeRequest = new AuthorizeOAuth2Request
        {
            ClientId = client.Id,
            RedirectUri = "https://externalhost/callback",
            ResponseType = OAuth2ResponseType.Code,
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
        };

        var authorized = await HttpApi.PostAsync(authorizeRequest.MakeApiRoute(),
            JsonContent.Create(authorizeRequest, options: _jsonOptions),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        authorized.StatusCode.Should().Be(HttpStatusCode.OK);
        var continuationCookie = authorized.GetCookie(CookieType.PendingOAuth2Authorization);
        continuationCookie.Should().NotBeNone();

        // Authenticate the user
        var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(JsonOptions, CSRFService);

        // Should now be able to utilize continuation from cookie
        authorizeRequest = new AuthorizeOAuth2Request();
        var result = await HttpApi.PostAsync(authorizeRequest.MakeApiRoute(),
            JsonContent.Create(authorizeRequest, options: _jsonOptions),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService, userId));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = result.Content.ReadFromJsonAsync<AuthorizeOAuth2Response>(JsonOptions).Result!;
        response.Redirect.IsLogin.Should().BeFalse();
        response.Redirect.IsConsent.Should().BeTrue();
        response.Redirect.IsExternal.Should().BeFalse();
        response.Redirect.RedirectUri.Should().Contain(WebsiteUiService.OAuth2ConsentPageRoute);
        result.GetCookie(CookieType.PendingOAuth2Authorization).Should().BeNone();
    }

    [Fact]
    public async Task WhenAuthorizeAfterAuthenticationWithContinuationAndConsented_ThenReturnsAuthorizationCode()
    {
        var client = await CreateClientAsync();
        var authorizeRequest = new AuthorizeOAuth2Request
        {
            ClientId = client.Id,
            RedirectUri = "https://externalhost/callback",
            ResponseType = OAuth2ResponseType.Code,
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
        };

        var authorized = await HttpApi.PostAsync(authorizeRequest.MakeApiRoute(),
            JsonContent.Create(authorizeRequest, options: _jsonOptions),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        authorized.StatusCode.Should().Be(HttpStatusCode.OK);
        var continuationCookie = authorized.GetCookie(CookieType.PendingOAuth2Authorization);
        continuationCookie.Should().NotBeNone();

        // Authenticate the user
        var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(JsonOptions, CSRFService);

        // Consent the client
        var consentRequest = new ConsentOAuth2ClientRequest
        {
            Id = client.Id,
            RedirectUri = "https://externalhost/callback",
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}",
            Consented = true
        };
        var consent = await HttpApi.PostAsync(consentRequest.MakeApiRoute(), JsonContent.Create(consentRequest),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService, userId));

        consent.StatusCode.Should().Be(HttpStatusCode.OK);
        consent.GetCookie(CookieType.PendingOAuth2Authorization).Should().Be(continuationCookie);

        // Should now be able to utilize continuation from cookie
        authorizeRequest = new AuthorizeOAuth2Request();
        var result = await HttpApi.PostAsync(authorizeRequest.MakeApiRoute(),
            JsonContent.Create(authorizeRequest, options: _jsonOptions),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService, userId));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = result.Content.ReadFromJsonAsync<AuthorizeOAuth2Response>(JsonOptions).Result!;
        response.Redirect.IsLogin.Should().BeFalse();
        response.Redirect.IsConsent.Should().BeFalse();
        response.Redirect.IsExternal.Should().BeTrue();
        response.Redirect.RedirectUri.Should().StartWith("https://externalhost/callback?code=");
        result.GetCookie(CookieType.PendingOAuth2Authorization).Should().BeNone();
    }

    [Fact]
    public async Task WhenDenyConsent_ThenReturnsErrorRedirect()
    {
        var client = await CreateClientAsync();
        var authorizeRequest = new AuthorizeOAuth2Request
        {
            ClientId = client.Id,
            RedirectUri = "https://externalhost/callback",
            ResponseType = OAuth2ResponseType.Code,
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
        };

        var authorized = await HttpApi.PostAsync(authorizeRequest.MakeApiRoute(),
            JsonContent.Create(authorizeRequest, options: _jsonOptions),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        authorized.StatusCode.Should().Be(HttpStatusCode.OK);
        var continuationCookie = authorized.GetCookie(CookieType.PendingOAuth2Authorization);
        continuationCookie.Should().NotBeNone();

        // Authenticate the user
        var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(JsonOptions, CSRFService);

        // Deny consent the client
        var consentRequest = new ConsentOAuth2ClientRequest
        {
            Id = client.Id,
            RedirectUri = "https://externalhost/callback",
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}",
            Consented = false
        };
        var consent = await HttpApi.PostAsync(consentRequest.MakeApiRoute(), JsonContent.Create(consentRequest),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService, userId));

        consent.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = consent.Content.ReadFromJsonAsync<ConsentOAuth2ClientResponse>(JsonOptions).Result!;
        response.Redirect.RedirectUri.Should()
            .StartWith("https://externalhost/callback?error=access_denied&error_description=");
        response.Redirect.IsConsented.Should().BeFalse();
        consent.GetCookie(CookieType.PendingOAuth2Authorization).Should().BeNone();
    }

    [Fact]
    public async Task WhenAcceptConsentAfterPreviousAuthorization_ThenReturnsConsent()
    {
        var client = await CreateClientAsync();
        var authorizeRequest = new AuthorizeOAuth2Request
        {
            ClientId = client.Id,
            RedirectUri = "https://externalhost/callback",
            ResponseType = OAuth2ResponseType.Code,
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
        };

        var authorized = await HttpApi.PostAsync(authorizeRequest.MakeApiRoute(),
            JsonContent.Create(authorizeRequest, options: _jsonOptions),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        authorized.StatusCode.Should().Be(HttpStatusCode.OK);
        var continuationCookie = authorized.GetCookie(CookieType.PendingOAuth2Authorization);
        continuationCookie.Should().NotBeNone();

        // Authenticate the user
        var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(JsonOptions, CSRFService);

        // Consent the client
        var consentRequest = new ConsentOAuth2ClientRequest
        {
            Id = client.Id,
            RedirectUri = "https://externalhost/callback",
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}",
            Consented = true
        };
        var consent = await HttpApi.PostAsync(consentRequest.MakeApiRoute(), JsonContent.Create(consentRequest),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService, userId));

        consent.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = consent.Content.ReadFromJsonAsync<ConsentOAuth2ClientResponse>(JsonOptions).Result!;
        response.Redirect.RedirectUri.Should().BeNull();
        response.Redirect.IsConsented.Should().BeTrue();
        consent.GetCookie(CookieType.PendingOAuth2Authorization).Should().Be(continuationCookie);
    }

    private async Task<OAuth2Client> CreateClientAsync()
    {
        var (userId, _) = await HttpApi.LoginOperatorFromBrowserAsync(JsonOptions, CSRFService);

        var request = new CreateOAuth2ClientRequest
        {
            Name = "aclientname",
            RedirectUri = "https://externalhost/callback"
        };
        var client = await HttpApi.PostAsync(request.MakeApiRoute(), JsonContent.Create(request),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService, userId));

        var auth2Client = (await client.Content.ReadFromJsonAsync<GetOAuth2ClientResponse>(JsonOptions))!.Client;

        await HttpApi.LogoutAsync(JsonOptions, CSRFService, userId);

        return auth2Client;
    }
}