using System.Diagnostics.CodeAnalysis;
using System.Net;
using ApiHost1;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;
#if TESTINGONLY
using Infrastructure.External.TestingOnly.ApplicationServices;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
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
    public async Task WhenAuthenticateAndWrongAuthCode_ThenReturnsError()
    {
        var result = await Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
#endif
            AuthCode = "awrongcode"
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAuthenticateAndNotYetRegistered_ThenReturnsNewTokens()
    {
        var result = await AuthenticateWhilePropagatingAsync(() => Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        }));

        result.Content.Value.Tokens.UserId.Should().NotBeNullOrEmpty();
        result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond().Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond()
                .Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
        _userNotificationsService.LastReRegistrationCourtesyEmailRecipient.Should().BeNull();

        var memberships = await Api.GetAsync(new ListMembershipsForCallerRequest(),
            req => req.SetJWTBearerToken(result.Content.Value.Tokens.AccessToken.Value));

        memberships.Content.Value.Memberships.Count.Should().Be(1);
        memberships.Content.Value.Memberships[0].OrganizationId.Should().NotBeNull();
        memberships.Content.Value.Memberships[0].Ownership.Should().Be(OrganizationOwnership.Personal);
    }

    [Fact]
    public async Task
        WhenAuthenticateWithSameEmailAndEndUserAlreadyRegisteredWithPassword_ThenReturnsSameUserNewTokens()
    {
#if TESTINGONLY
        var authCode = FakeOAuth2Service.AuthCode1;
        var emailAddress = FakeOAuth2Service.ValidAuthCodes[authCode];
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

        var result = await AuthenticateWhilePropagatingAsync(() => Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        }));

        result.Content.Value.Tokens.UserId.Should().Be(userId);
        result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond().Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
        result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
            .BeNear(DateTime.UtcNow.ToNearestSecond().Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
        _userNotificationsService.LastReRegistrationCourtesyEmailRecipient.Should().BeNull();
#endif
    }

    [Fact]
    public async Task WhenAuthenticateWithSameEmailAndSSOUserAlreadyExists_ThenReturnsSameUserNewTokens()
    {
        var authenticated = await AuthenticateWhilePropagatingAsync(() =>
            Api.PostAsync(new AuthenticateSingleSignOnRequest
            {
#if TESTINGONLY
                Provider = FakeSSOAuthenticationProvider.SSOName,
                AuthCode = FakeOAuth2Service.AuthCode1
#endif
            }));

        authenticated.Content.Value.Tokens.UserId.Should().NotBeNullOrEmpty();
        var userId = authenticated.Content.Value.Tokens.UserId;

        var result = await AuthenticateWhilePropagatingAsync(() => Api.PostAsync(new AuthenticateSingleSignOnRequest
        {
#if TESTINGONLY
            Provider = FakeSSOAuthenticationProvider.SSOName,
            AuthCode = FakeOAuth2Service.AuthCode1
#endif
        }));

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
        var authenticate = await AuthenticateWhilePropagatingAsync(() =>
            Api.PostAsync(new AuthenticateSingleSignOnRequest
            {
#if TESTINGONLY
                Provider = FakeSSOAuthenticationProvider.SSOName,
                AuthCode = FakeOAuth2Service.AuthCode1
#endif
            }));

#if TESTINGONLY
        var accessToken = authenticate.Content.Value.Tokens.AccessToken.Value;

        var result = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetJWTBearerToken(accessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(authenticate.Content.Value.Tokens.UserId);
#endif
    }

    /// <summary>
    ///     Executes an API call while continuously propagating domain events in the background, until the API call returns
    /// </summary>
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    private async Task<TResult> AuthenticateWhilePropagatingAsync<TResult>(Func<Task<TResult>> apiMethod,
        int propagationDelay = 200)
    {
        using var pollingToken = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);

        var apiTask = Task.Run(async () =>
        {
            var result = await apiMethod();
            await pollingToken.CancelAsync();
            return result;
        }, pollingToken.Token);

        var pollingTask = Task.Run(async () =>
        {
            while (!pollingToken.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(propagationDelay, pollingToken.Token);
                    await PropagateDomainEventsAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, pollingToken.Token);

        await Task.WhenAll(apiTask, pollingTask);

        return await apiTask;
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // do nothing
    }
}