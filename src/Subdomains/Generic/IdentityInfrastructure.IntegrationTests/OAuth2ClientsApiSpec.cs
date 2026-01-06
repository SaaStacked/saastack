using System.Net;
using ApiHost1;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class OAuth2ClientsApiSpec : WebApiSpec<Program>
{
    public OAuth2ClientsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenCreateClientAndUnauthenticated_ThenReturnsUnauthorized()
    {
        var result = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenCreateClientAndAuthenticated_ThenCreatesClient()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);

        var result = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname",
            RedirectUri = "https://localhost/callback"
        }, req => req.SetJWTBearerToken(@operator.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Client.Should().NotBeNull();
        result.Content.Value.Client.Id.Should().NotBeEmpty();
        result.Content.Value.Client.Name.Should().Be("aclientname");
        result.Content.Value.Client.RedirectUri.Should().Be("https://localhost/callback");
    }

    [Fact]
    public async Task WhenGetClient_ThenReturnsClient()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.GetAsync(new GetOAuth2ClientRequest
        {
            Id = client.Id
        }, req => req.SetJWTBearerToken(@operator.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Client.Id.Should().Be(client.Id);
        result.Content.Value.Client.Name.Should().Be("aclientname");
    }

    [Fact]
    public async Task WhenUpdateClient_ThenUpdatesClient()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.PutAsync(new UpdateOAuth2ClientRequest
        {
            Id = client.Id,
            Name = "anotherclientname",
            RedirectUri = "https://localhost/callback"
        }, req => req.SetJWTBearerToken(@operator.AccessToken));

        result.Content.Value.Client.Should().NotBeNull();
        result.Content.Value.Client.Id.Should().Be(client.Id);
        result.Content.Value.Client.Name.Should().Be("anotherclientname");
        result.Content.Value.Client.RedirectUri.Should().Be("https://localhost/callback");
    }

    [Fact]
    public async Task WhenDeleteClient_ThenDeletesClient()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.DeleteAsync(new DeleteOAuth2ClientRequest
        {
            Id = client.Id
        }, req => req.SetJWTBearerToken(@operator.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var clients = await Api.GetAsync(new SearchAllOAuth2ClientsRequest(),
            req => req.SetJWTBearerToken(@operator.AccessToken));

        clients.Content.Value.Clients.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenSearchAllClients_ThenReturnsClients()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var client1 = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname1"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;
        var client2 = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname2"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var clients = await Api.GetAsync(new SearchAllOAuth2ClientsRequest(),
            req => req.SetJWTBearerToken(@operator.AccessToken));

        clients.Content.Value.Clients.Count.Should().Be(2);
        clients.Content.Value.Clients.Should().Contain(c => c.Id == client1.Id);
        clients.Content.Value.Clients.Should().Contain(c => c.Id == client2.Id);
    }

    [Fact]
    public async Task WhenRegenerateClientSecret_ThenGeneratesSecret()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.PostAsync(new RegenerateOAuth2ClientSecretRequest
        {
            Id = client.Id
        }, req => req.SetJWTBearerToken(@operator.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Client!.Id.Should().Be(client.Id);
        result.Content.Value.Client.Name.Should().Be("aclientname");
        result.Content.Value.Client.Secret.Should().NotBeEmpty();
        result.Content.Value.Client.ExpiresOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task WhenConsentClientForCallerAndUnauthenticated_ThenReturnsUnauthorized()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.PostAsync(new ConsentOAuth2ClientForCallerRequest
        {
            Id = client.Id,
            RedirectUri = "https://localhost/callback",
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
            Consented = true
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenConsentClientForCallerWithoutOpenIdScope_ThenReturnsBadRequest()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var user = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.PostAsync(new ConsentOAuth2ClientForCallerRequest
        {
            Id = client.Id,
            RedirectUri = "https://localhost/callback",
            Scope = $"{OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
            Consented = true
        }, req => req.SetJWTBearerToken(user.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Content.Error.Title.Should().Be(OAuth2Constants.ErrorCodes.InvalidScope);
    }

    [Fact]
    public async Task WhenUnConsentClientForCaller_ThenUnConsentsClientAndReturnsRedirect()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var user = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.PostAsync(new ConsentOAuth2ClientForCallerRequest
        {
            Id = client.Id,
            RedirectUri = "https://localhost/callback",
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
            Consented = false,
            State = "astate"
        }, req => req.SetJWTBearerToken(user.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Found);
        result.Headers.Location!.Query.Should()
            .Be(
                $"?error={OAuth2Constants.ErrorCodes.AccessDenied}&error_description=The%20user%20has%20denied%20access%20to%20the%20client%20application&state=astate");
        result.Content.Value.Consent.Should().BeNull();
    }
    
    [Fact]
    public async Task WhenConsentClientForCaller_ThenConsentsToClient()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var user = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.PostAsync(new ConsentOAuth2ClientForCallerRequest
        {
            Id = client.Id,
            RedirectUri = "https://localhost/callback",
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
            Consented = true
        }, req => req.SetJWTBearerToken(user.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Consent!.ClientId.Should().Be(client.Id);
        result.Content.Value.Consent.UserId.Should().Be(user.User.Id);
        result.Content.Value.Consent.IsConsented.Should().BeTrue();
        result.Content.Value.Consent.Scopes.Should().BeEquivalentTo(new List<string>
        {
            OpenIdConnectConstants.Scopes.OpenId,
            OAuth2Constants.Scopes.Profile,
            OAuth2Constants.Scopes.Email
        });
    }

    [Fact]
    public async Task WhenGetClientConsentStatusForCallerAndNotExists_ThenReturnsConsentStatus()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var user = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.GetAsync(new GetOAuth2ClientConsentStatusForCallerRequest
        {
            Id = client.Id,
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}"
        }, req => req.SetJWTBearerToken(user.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Status.IsConsented.Should().BeFalse();
    }

    [Fact]
    public async Task WhenGetClientConsentStatusForCallerAndConsented_ThenReturnsConsentStatus()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var user = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        await Api.PostAsync(new ConsentOAuth2ClientForCallerRequest
        {
            Id = client.Id,
            RedirectUri = "https://localhost/callback",
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
            Consented = true
        }, req => req.SetJWTBearerToken(user.AccessToken));

        var result = await Api.GetAsync(new GetOAuth2ClientConsentStatusForCallerRequest
        {
            Id = client.Id,
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}"
        }, req => req.SetJWTBearerToken(user.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Status.IsConsented.Should().BeTrue();
    }

    [Fact]
    public async Task WhenGetClientConsentForCaller_ThenReturnsConsent()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var user = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        await Api.PostAsync(new ConsentOAuth2ClientForCallerRequest
        {
            Id = client.Id,
            RedirectUri = "https://localhost/callback",
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
            Consented = true
        }, req => req.SetJWTBearerToken(user.AccessToken));

        var result = await Api.GetAsync(new GetOAuth2ClientConsentForCallerRequest
        {
            Id = client.Id
        }, req => req.SetJWTBearerToken(user.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Consent!.ClientId.Should().Be(client.Id);
        result.Content.Value.Consent.UserId.Should().Be(user.User.Id);
        result.Content.Value.Consent.IsConsented.Should().BeTrue();
        result.Content.Value.Consent.Scopes.Should().BeEquivalentTo(new List<string>
        {
            OpenIdConnectConstants.Scopes.OpenId,
            OAuth2Constants.Scopes.Profile,
            OAuth2Constants.Scopes.Email
        });
    }

    [Fact]
    public async Task WhenGetClientConsentForCallerAndUnauthenticated_ThenReturnsUnauthorized()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.GetAsync(new GetOAuth2ClientConsentForCallerRequest
        {
            Id = client.Id
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenRevokeClientConsentForCaller_ThenRevokesConsent()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var user = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        await Api.PostAsync(new ConsentOAuth2ClientForCallerRequest
        {
            Id = client.Id,
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
            Consented = true
        }, req => req.SetJWTBearerToken(user.AccessToken));

        var result = await Api.DeleteAsync(new RevokeOAuth2ClientConsentForCallerRequest
        {
            Id = client.Id
        }, req => req.SetJWTBearerToken(user.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var consent = await Api.GetAsync(new GetOAuth2ClientConsentForCallerRequest
        {
            Id = client.Id
        }, req => req.SetJWTBearerToken(user.AccessToken));

        consent.Content.Value.Consent!.ClientId.Should().Be(client.Id);
        consent.Content.Value.Consent.UserId.Should().Be(user.User.Id);
        consent.Content.Value.Consent.IsConsented.Should().BeFalse();
        consent.Content.Value.Consent.Scopes.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenRevokeClientConsentForCallerAndUnauthenticated_ThenReturnsUnauthorized()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(@operator.AccessToken))).Content.Value.Client;

        var result = await Api.DeleteAsync(new RevokeOAuth2ClientConsentForCallerRequest
        {
            Id = client.Id
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}