using System.Net;
using ApiHost1;
using Application.Interfaces;
using Application.Services.Shared;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;
#if TESTINGONLY
using Infrastructure.External.TestingOnly.ApplicationServices;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Common.Extensions;
#endif

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class SingleSignOnApiSpec : WebApiSpec<Program>
{
    private readonly StubUserNotificationsService _userNotificationsService;

    public SingleSignOnApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _userNotificationsService =
            setup.GetRequiredService<IUserNotificationsService>().As<StubUserNotificationsService>();
        _userNotificationsService.Reset();
    }

    [Fact]
    public async Task WhenAuthenticateAndUnknownProvider_ThenReturnsError()
    {
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Provider = "anunknownprovider",
#if TESTINGONLY
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAuthenticateAndNoUsername_ThenReturnsError()
    {
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = null,
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAuthenticateAndWrongAuthCode_ThenReturnsError()
    {
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = "auser@company.com",
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
#endif
            AuthCode = "awrongcode"
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAuthenticateWithNewEmail_ThenRegistersNewUser()
    {
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = "auser@company.com",
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        });

        result.Content.Value.Tokens.UserId.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond().Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond().Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
    }

    [Fact]
    public async Task
        WhenAuthenticateWithSameEmailAndEndUserAlreadyRegisteredWithPassword_ThenReturnsSameUserNewTokens()
    {
        const string emailAddress = "auser@company.com";
        var registered = await Api.PostAsync(new RegisterPersonCredentialRequest
        {
            EmailAddress = emailAddress,
            FirstName = "afirstname",
            LastName = "alastname",
            Password = "1Password!",
            TermsAndConditionsAccepted = true
        });

        var userId = registered.Content.Value.Person.User.Id;

        await PropagateDomainEventsAsync();
        var token = UserNotificationsService.LastRegistrationConfirmationToken;
        await Api.PostAsync(new ConfirmPersonCredentialRegistrationRequest
        {
            Token = token!
        });

        await PropagateDomainEventsAsync();
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = emailAddress,
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        });

        result.Content.Value.Tokens.UserId.Should().Be(userId);
        result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond().Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond().Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
        _userNotificationsService.LastReRegistrationCourtesyEmailRecipient.Should().BeNull();
    }

    [Fact]
    public async Task WhenAuthenticateWithSameEmailAndSSOUserAlreadyExists_ThenReturnsSameUserNewTokens()
    {
        const string emailAddress = "auser@company.com";
        var authenticated = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = emailAddress,
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        });

        authenticated.Content.Value.Tokens.UserId.Should().NotBeNullOrEmpty();
        var userId = authenticated.Content.Value.Tokens.UserId;

        await PropagateDomainEventsAsync();
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = emailAddress,
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        });

        result.Content.Value.Tokens.UserId.Should().Be(userId);
        result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond().Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond().Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
        _userNotificationsService.LastReRegistrationCourtesyEmailRecipient.Should().BeNull();
    }

    [Fact]
    public async Task WhenCallingSecureApiAfterAuthenticate_ThenReturnsResponse()
    {
        var authenticate = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
            Username = "auser@company.com",
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        });

#if TESTINGONLY
        var accessToken = authenticate.Content.Value.Tokens.AccessToken.Value;

        var result = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetJWTBearerToken(accessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(authenticate.Content.Value.Tokens.UserId);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // do nothing
    }
}