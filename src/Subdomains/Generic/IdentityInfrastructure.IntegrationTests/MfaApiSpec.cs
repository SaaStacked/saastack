using System.Net;
using System.Text.Json;
using ApiHost1;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common.Configuration;
using Domain.Services.Shared;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using IdentityDomain.DomainServices;
using IdentityInfrastructure.ApplicationServices;
using IdentityInfrastructure.IntegrationTests.Stubs;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[UsedImplicitly]
public class MfaApiSpec
{
    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAnUnauthenticatedUser : WebApiSpec<Program>
    {
        private readonly StubMfaService _mfaService;
        private readonly StubUserNotificationsService _userNotificationsService;

        public GivenAnUnauthenticatedUser(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
        {
            EmptyAllRepositories();
            _userNotificationsService =
                setup.GetRequiredService<IUserNotificationsService>().As<StubUserNotificationsService>();
            _userNotificationsService.Reset();
            _mfaService = setup.GetRequiredService<IMfaService>().As<StubMfaService>();
            _mfaService.Reset();
        }

        [Fact]
        public async Task WhenListMfaAuthenticatorsWithNone_ThenReturnsEmpty()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var result = await Api.GetAsync(new ListCredentialMfaAuthenticatorsForCallerRequest
            {
                MfaToken = mfaToken
            });

            result.Content.Value.Authenticators.Count.Should().Be(0);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorWithOtpAuthenticator_ThenAssociates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.TotpAuthenticator,
                PhoneNumber = null
            });

            result.Content.Value.Authenticator.Type.Should()
                .Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
            result.Content.Value.Authenticator.RecoveryCodes.Should().NotBeEmpty();
            result.Content.Value.Authenticator.OobCode.Should().BeNull();
            result.Content.Value.Authenticator.BarCodeUri.Should().StartWith("otpauth://totp/");

            var authenticators = await GetAuthenticators(mfaToken);
            authenticators.Count.Should().Be(2);
            authenticators[0].Id.Should().NotBeEmpty();
            authenticators[0].IsActive.Should().BeTrue();
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Id.Should().NotBeEmpty();
            authenticators[1].IsActive.Should().BeFalse();
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorWithOobSms_ThenAssociates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.OobSms,
                PhoneNumber = "+6498876986"
            });

            result.Content.Value.Authenticator.Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
            result.Content.Value.Authenticator.RecoveryCodes.Should().NotBeEmpty();
            result.Content.Value.Authenticator.OobCode.Should().NotBeNullOrEmpty();
            result.Content.Value.Authenticator.BarCodeUri.Should().BeNull();
            _userNotificationsService.LastMfaOobSmsRecipient.Should().Be("+6498876986");
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();

            var authenticators = await GetAuthenticators(mfaToken);
            authenticators.Count.Should().Be(2);
            authenticators[0].Id.Should().NotBeEmpty();
            authenticators[0].IsActive.Should().BeTrue();
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Id.Should().NotBeEmpty();
            authenticators[1].IsActive.Should().BeFalse();
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorWithOobEmail_ThenAssociates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.OobEmail,
                PhoneNumber = null
            });

            result.Content.Value.Authenticator.Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
            result.Content.Value.Authenticator.RecoveryCodes.Should().NotBeEmpty();
            result.Content.Value.Authenticator.OobCode.Should().NotBeNullOrEmpty();
            result.Content.Value.Authenticator.BarCodeUri.Should().BeNull();
            _userNotificationsService.LastMfaOobEmailRecipient.Should().Be(login.Profile!.EmailAddress);
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();

            var authenticators = await GetAuthenticators(mfaToken);
            authenticators.Count.Should().Be(2);
            authenticators[0].Id.Should().NotBeEmpty();
            authenticators[0].IsActive.Should().BeTrue();
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Id.Should().NotBeEmpty();
            authenticators[1].IsActive.Should().BeFalse();
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorAgainForSameAuthenticator_ThenUpdatesAssociation()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            await Associate(CredentialMfaAuthenticatorType.OobSms, mfaToken, "+6498876981");

            _userNotificationsService.LastMfaOobSmsRecipient.Should().Be("+6498876981");
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();

            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.OobSms,
                PhoneNumber = "+6498876982"
            });

            result.Content.Value.Authenticator.Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
            result.Content.Value.Authenticator.RecoveryCodes.Should().NotBeEmpty();
            result.Content.Value.Authenticator.OobCode.Should().NotBeNullOrEmpty();
            result.Content.Value.Authenticator.BarCodeUri.Should().BeNull();
            _userNotificationsService.LastMfaOobSmsRecipient.Should().Be("+6498876982");
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();

            var authenticators = await GetAuthenticators(mfaToken);
            authenticators.Count.Should().Be(2);
            authenticators[0].Id.Should().NotBeEmpty();
            authenticators[0].IsActive.Should().BeTrue();
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Id.Should().NotBeEmpty();
            authenticators[1].IsActive.Should().BeFalse();
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorAgainForAnotherAuthenticator_ThenForbids()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken);
            var confirmationCode = _mfaService.GetOtpCodeNow();
            await Confirm(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken, null, confirmationCode);
            mfaToken = await AttemptAuthenticationToGetMfaToken(login);

            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.OobSms,
                PhoneNumber = "+6498876986"
            });

            result.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public async Task WhenConfirmMfaAuthenticatorAssociationWithOtpAuthenticator_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken);

            var confirmationCode = _mfaService.GetOtpCodeNow();
            var result = await Api.PutAsync(new ConfirmCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.TotpAuthenticator,
                ConfirmationCode = confirmationCode
            });

            result.Content.Value.Tokens!.AccessToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
            result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
        }

        [Fact]
        public async Task WhenConfirmMfaAuthenticatorAssociationWithOobSms_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var (oobCode, confirmationCode, _) =
                await Associate(CredentialMfaAuthenticatorType.OobSms, mfaToken);

            var result = await Api.PutAsync(new ConfirmCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.OobSms,
                OobCode = oobCode,
                ConfirmationCode = confirmationCode
            });

            result.Content.Value.Tokens!.AccessToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
            result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
        }

        [Fact]
        public async Task WhenConfirmMfaAuthenticatorAssociationWithOobEmail_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var (oobCode, confirmationCode, _) =
                await Associate(CredentialMfaAuthenticatorType.OobEmail, mfaToken);

            var result = await Api.PutAsync(new ConfirmCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.OobEmail,
                OobCode = oobCode,
                ConfirmationCode = confirmationCode
            });

            result.Content.Value.Tokens!.AccessToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
            result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry));
        }

        [Fact]
        public async Task WhenChallengeMfaAuthenticatorWithOtpAuthenticator_ThenChallenges()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken);
            var confirmationCode = _mfaService.GetOtpCodeNow();
            await Confirm(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken, null, confirmationCode);
            mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var authenticator =
                await GetAuthenticator(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken);

            var result = await Api.PutAsync(new ChallengeCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorId = authenticator!.Id
            });

            result.Content.Value.Challenge.Type.Should().Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
            result.Content.Value.Challenge.OobCode.Should().BeNull();
            _userNotificationsService.LastMfaOobSmsRecipient.Should().BeNull();
            _userNotificationsService.LastMfaOobEmailRecipient.Should().BeNull();
        }

        [Fact]
        public async Task WhenChallengeMfaAuthenticatorWithOobSms_ThenChallenges()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var (oobCode, confirmationCode, _) =
                await Associate(CredentialMfaAuthenticatorType.OobSms, mfaToken);
            await Confirm(CredentialMfaAuthenticatorType.OobSms, mfaToken, oobCode, confirmationCode);
            mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var authenticator = await GetAuthenticator(CredentialMfaAuthenticatorType.OobSms, mfaToken);

            var result = await Api.PutAsync(new ChallengeCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorId = authenticator!.Id
            });

            result.Content.Value.Challenge.Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
            result.Content.Value.Challenge.OobCode.Should().NotBeNullOrEmpty();
            _userNotificationsService.LastMfaOobSmsRecipient.Should().Be("+6498876986");
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();
        }

        [Fact]
        public async Task WhenChallengeMfaAuthenticatorWithOobEmail_ThenChallenges()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var (oobCode, confirmationCode, _) =
                await Associate(CredentialMfaAuthenticatorType.OobEmail, mfaToken);
            await Confirm(CredentialMfaAuthenticatorType.OobEmail, mfaToken, oobCode, confirmationCode);
            mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var authenticator = await GetAuthenticator(CredentialMfaAuthenticatorType.OobEmail, mfaToken);

            var result = await Api.PutAsync(new ChallengeCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorId = authenticator!.Id
            });

            result.Content.Value.Challenge.Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
            result.Content.Value.Challenge.OobCode.Should().NotBeNullOrEmpty();
            _userNotificationsService.LastMfaOobEmailRecipient.Should().Be(login.Profile!.EmailAddress);
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();
        }

        [Fact]
        public async Task WhenVerifyMfaAuthenticatorWithOtpAuthenticator_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken);
            var confirmationCode = _mfaService.GetOtpCodeNow();
            await Confirm(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken, null, confirmationCode);
            mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var authenticator =
                await GetAuthenticator(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken);
            await Challenge(mfaToken, authenticator);
            confirmationCode = _mfaService.GetOtpCodeNow(MfaService.TimeStep.Next); //One time step ahead

            var result = await Api.PutAsync(new VerifyCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.TotpAuthenticator,
                OobCode = null,
                ConfirmationCode = confirmationCode
            });

            result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry),
                    TimeSpan.FromMinutes(1));
            result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry),
                    TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task WhenVerifyMfaAuthenticatorWithOobSms_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var (oobCode, confirmationCode, _) =
                await Associate(CredentialMfaAuthenticatorType.OobSms, mfaToken);
            await Confirm(CredentialMfaAuthenticatorType.OobSms, mfaToken, oobCode, confirmationCode);
            mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var authenticator = await GetAuthenticator(CredentialMfaAuthenticatorType.OobSms, mfaToken);
            (oobCode, confirmationCode) = await Challenge(mfaToken, authenticator);

            var result = await Api.PutAsync(new VerifyCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.OobSms,
                OobCode = oobCode,
                ConfirmationCode = confirmationCode
            });

            result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry),
                    TimeSpan.FromMinutes(1));
            result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry),
                    TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task WhenVerifyMfaAuthenticatorWithOobEmail_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var (oobCode, confirmationCode, _) =
                await Associate(CredentialMfaAuthenticatorType.OobEmail, mfaToken);
            await Confirm(CredentialMfaAuthenticatorType.OobEmail, mfaToken, oobCode, confirmationCode);
            mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var authenticator = await GetAuthenticator(CredentialMfaAuthenticatorType.OobEmail, mfaToken);
            (oobCode, confirmationCode) = await Challenge(mfaToken, authenticator);

            var result = await Api.PutAsync(new VerifyCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.OobEmail,
                OobCode = oobCode,
                ConfirmationCode = confirmationCode
            });

            result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry),
                    TimeSpan.FromMinutes(1));
            result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry),
                    TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task WhenVerifyMfaAuthenticatorWithARecoveryCode_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var (_, _, recoveryCodes) =
                await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken);
            var confirmationCode = _mfaService.GetOtpCodeNow();
            await Confirm(CredentialMfaAuthenticatorType.TotpAuthenticator, mfaToken, null, confirmationCode);
            mfaToken = await AttemptAuthenticationToGetMfaToken(login);
            var authenticator = await GetAuthenticator(CredentialMfaAuthenticatorType.RecoveryCodes, mfaToken);
            await Challenge(mfaToken, authenticator);

            var result = await Api.PutAsync(new VerifyCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = CredentialMfaAuthenticatorType.RecoveryCodes,
                OobCode = null,
                ConfirmationCode = recoveryCodes![0]
            });

            result.Content.Value.Tokens.AccessToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.AccessToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry),
                    TimeSpan.FromMinutes(1));
            result.Content.Value.Tokens.RefreshToken.Value.Should().NotBeNull();
            result.Content.Value.Tokens.RefreshToken.ExpiresOn.Should()
                .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry),
                    TimeSpan.FromMinutes(1));
        }

        private async Task<(string OobCode, string ConfirmationCode)> Challenge(string mfaToken,
            CredentialMfaAuthenticator? authenticator)
        {
            var challenged = await Api.PutAsync(new ChallengeCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorId = authenticator!.Id
            });

            var oobCode = challenged.Content.Value.Challenge.OobCode!;
            var confirmationCode = _mfaService.LastOobConfirmationCode!;

            return (oobCode, confirmationCode);
        }

        private async Task<CredentialMfaAuthenticator?> GetAuthenticator(
            CredentialMfaAuthenticatorType type, string mfaToken)
        {
            var authenticators = await Api.GetAsync(new ListCredentialMfaAuthenticatorsForCallerRequest
            {
                MfaToken = mfaToken
            });

            return authenticators.Content.Value.Authenticators
                .FirstOrDefault(auth => auth.Type == type);
        }

        private async Task<List<CredentialMfaAuthenticator>> GetAuthenticators(string mfaToken)
        {
            var authenticators = await Api.GetAsync(new ListCredentialMfaAuthenticatorsForCallerRequest
            {
                MfaToken = mfaToken
            });

            return authenticators.Content.Value.Authenticators;
        }

        private async Task Confirm(CredentialMfaAuthenticatorType type, string mfaToken, string? oobCode = null,
            string? confirmationCode = null)
        {
            await Api.PutAsync(new ConfirmCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = type,
                OobCode = oobCode,
                ConfirmationCode = confirmationCode
            });
        }

        private async Task<(string OobCode, string ConfirmationCode, List<string>? recoveryCodes)> Associate(
            CredentialMfaAuthenticatorType type, string mfaToken, string phoneNumber = "+6498876986")
        {
            var associated = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
            {
                MfaToken = mfaToken,
                AuthenticatorType = type,
                PhoneNumber = type == CredentialMfaAuthenticatorType.OobSms
                    ? phoneNumber
                    : null
            });

            var oobCode = associated.Content.Value.Authenticator.OobCode!;
            var confirmationCode = _mfaService.LastOobConfirmationCode!;
            var recoveryCodes = associated.Content.Value.Authenticator.RecoveryCodes;

            return (oobCode, confirmationCode, recoveryCodes);
        }

        private async Task<string> AttemptAuthenticationToGetMfaToken(LoginDetails login)
        {
            var failedAuth = await Api.PostAsync(new AuthenticateCredentialRequest
            {
                Username = login.Profile!.EmailAddress,
                Password = PasswordForPerson
            });

            return failedAuth.Content.Error.Extensions![NativeIdentityServerCredentialsService.MfaTokenName]
                .As<JsonElement>().GetString()!;
        }

        private async Task EnableMfa(LoginDetails login)
        {
            await Api.PutAsync(new ChangeCredentialMfaForCallerRequest
            {
                IsEnabled = true
            }, req => req.SetJWTBearerToken(login.AccessToken));
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
            services.AddSingleton<IMfaService>(c =>
                new StubMfaService(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                    c.GetRequiredService<ITokensService>()));
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAnAuthenticatedUser : WebApiSpec<Program>
    {
        private readonly StubMfaService _mfaService;
        private readonly StubUserNotificationsService _userNotificationsService;

        public GivenAnAuthenticatedUser(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
        {
            EmptyAllRepositories();
            _userNotificationsService =
                setup.GetRequiredService<IUserNotificationsService>().As<StubUserNotificationsService>();
            _userNotificationsService.Reset();
            _mfaService = setup.GetRequiredService<IMfaService>().As<StubMfaService>();
            _mfaService.Reset();
        }

        [Fact]
        public async Task WhenEnableMfa_ThenEnables()
        {
            var login = await LoginUserAsync();

            var result = await Api.PutAsync(new ChangeCredentialMfaForCallerRequest
            {
                IsEnabled = true
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Credential.Id.Should().NotBeEmpty();
            result.Content.Value.Credential.IsMfaEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDisableMfa_ThenDisables()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);

            var result = await Api.PutAsync(new ChangeCredentialMfaForCallerRequest
            {
                IsEnabled = false
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Credential.Id.Should().NotBeEmpty();
            result.Content.Value.Credential.IsMfaEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDisableMfa_ThenDeletesAllAuthenticatorsAndDisables()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, login);
            var confirmationCode = _mfaService.GetOtpCodeNow();
            await Confirm(CredentialMfaAuthenticatorType.TotpAuthenticator, login, null, confirmationCode);

            var result = await Api.PutAsync(new ChangeCredentialMfaForCallerRequest
            {
                IsEnabled = false
            }, req => req.SetJWTBearerToken(login.AccessToken));

            await EnableMfa(login);

            result.Content.Value.Credential.IsMfaEnabled.Should().BeFalse();
            var authenticators = await GetAuthenticators(login);
            authenticators.Count.Should().Be(0);
        }

        [Fact]
        public async Task WhenListMfaAuthenticatorsWithNone_ThenReturnsEmpty()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var result = await Api.GetAsync(new ListCredentialMfaAuthenticatorsForCallerRequest(),
                req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Authenticators.Count.Should().Be(0);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorWithOtpAuthenticator_ThenAssociates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = CredentialMfaAuthenticatorType.TotpAuthenticator,
                    PhoneNumber = null
                },
                req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Authenticator.Type.Should()
                .Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
            result.Content.Value.Authenticator.RecoveryCodes.Should().NotBeEmpty();
            result.Content.Value.Authenticator.OobCode.Should().BeNull();
            result.Content.Value.Authenticator.BarCodeUri.Should().StartWith("otpauth://totp/");

            var authenticators = await GetAuthenticators(login);
            authenticators.Count.Should().Be(2);
            authenticators[0].Id.Should().NotBeEmpty();
            authenticators[0].IsActive.Should().BeTrue();
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Id.Should().NotBeEmpty();
            authenticators[1].IsActive.Should().BeFalse();
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorWithOobSms_ThenAssociates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = CredentialMfaAuthenticatorType.OobSms,
                    PhoneNumber = "+6498876986"
                },
                req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Authenticator.Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
            result.Content.Value.Authenticator.RecoveryCodes.Should().NotBeEmpty();
            result.Content.Value.Authenticator.OobCode.Should().NotBeNullOrEmpty();
            result.Content.Value.Authenticator.BarCodeUri.Should().BeNull();
            _userNotificationsService.LastMfaOobSmsRecipient.Should().Be("+6498876986");
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();

            var authenticators = await GetAuthenticators(login);
            authenticators.Count.Should().Be(2);
            authenticators[0].Id.Should().NotBeEmpty();
            authenticators[0].IsActive.Should().BeTrue();
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Id.Should().NotBeEmpty();
            authenticators[1].IsActive.Should().BeFalse();
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorWithOobEmail_ThenAssociates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = CredentialMfaAuthenticatorType.OobEmail,
                    PhoneNumber = null
                },
                req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Authenticator.Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
            result.Content.Value.Authenticator.RecoveryCodes.Should().NotBeEmpty();
            result.Content.Value.Authenticator.OobCode.Should().NotBeNullOrEmpty();
            result.Content.Value.Authenticator.BarCodeUri.Should().BeNull();
            _userNotificationsService.LastMfaOobEmailRecipient.Should().Be(login.Profile!.EmailAddress);
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();

            var authenticators = await GetAuthenticators(login);
            authenticators.Count.Should().Be(2);
            authenticators[0].Id.Should().NotBeEmpty();
            authenticators[0].IsActive.Should().BeTrue();
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Id.Should().NotBeEmpty();
            authenticators[1].IsActive.Should().BeFalse();
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorAgainForSameAuthenticator_ThenUpdatesAssociation()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            await Associate(CredentialMfaAuthenticatorType.OobSms, login, "+6498876981");

            _userNotificationsService.LastMfaOobSmsRecipient.Should().Be("+6498876981");
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();

            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = CredentialMfaAuthenticatorType.OobSms,
                    PhoneNumber = "+6498876982"
                },
                req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Authenticator.Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
            result.Content.Value.Authenticator.RecoveryCodes.Should().NotBeEmpty();
            result.Content.Value.Authenticator.OobCode.Should().NotBeNullOrEmpty();
            result.Content.Value.Authenticator.BarCodeUri.Should().BeNull();
            _userNotificationsService.LastMfaOobSmsRecipient.Should().Be("+6498876982");
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();

            var authenticators = await GetAuthenticators(login);
            authenticators.Count.Should().Be(2);
            authenticators[0].Id.Should().NotBeEmpty();
            authenticators[0].IsActive.Should().BeTrue();
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Id.Should().NotBeEmpty();
            authenticators[1].IsActive.Should().BeFalse();
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
        }

        [Fact]
        public async Task WhenAssociateMfaAuthenticatorAgainForAnotherAuthenticator_ThenAssociates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, login);
            var confirmationCode = _mfaService.GetOtpCodeNow();
            await Confirm(CredentialMfaAuthenticatorType.TotpAuthenticator, login, null, confirmationCode);

            var result = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = CredentialMfaAuthenticatorType.OobSms,
                    PhoneNumber = "+6498876986"
                },
                req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Authenticator.Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
            result.Content.Value.Authenticator.RecoveryCodes.Should().BeNull();
            result.Content.Value.Authenticator.OobCode.Should().NotBeNullOrEmpty();
            result.Content.Value.Authenticator.BarCodeUri.Should().BeNull();
            _userNotificationsService.LastMfaOobSmsRecipient.Should().Be("+6498876986");
            _userNotificationsService.LastMfaOobCode.Should().NotBeEmpty();

            var authenticators = await GetAuthenticators(login);
            authenticators.Count.Should().Be(3);
            authenticators[0].Id.Should().NotBeEmpty();
            authenticators[0].IsActive.Should().BeTrue();
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Id.Should().NotBeEmpty();
            authenticators[1].IsActive.Should().BeTrue();
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
            authenticators[2].Id.Should().NotBeEmpty();
            authenticators[2].IsActive.Should().BeFalse();
            authenticators[2].Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
        }

        [Fact]
        public async Task
            WhenDisassociateMfaAuthenticatorForFirstAuthenticator_ThenDeletesAuthenticatorAndRecoveryCodes()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, login);
            var confirmationCode = _mfaService.GetOtpCodeNow();
            await Confirm(CredentialMfaAuthenticatorType.TotpAuthenticator, login, null, confirmationCode);

            var authenticator = await GetAuthenticator(CredentialMfaAuthenticatorType.TotpAuthenticator, login);
            var result = await Api.DeleteAsync(new DisassociateCredentialMfaAuthenticatorForCallerRequest
            {
                Id = authenticator!.Id
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var authenticators = await GetAuthenticators(login);
            authenticators.Count.Should().Be(0);
        }

        [Fact]
        public async Task WhenDisassociateMfaAuthenticatorForSecondAuthenticator_ThenDeletesAuthenticatorOnly()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, login);
            var otpConfirmationCode = _mfaService.GetOtpCodeNow();
            await Confirm(CredentialMfaAuthenticatorType.TotpAuthenticator, login, null, otpConfirmationCode);
            var (oobCode, ooBConfirmationCode, _) =
                await Associate(CredentialMfaAuthenticatorType.OobSms, login);
            await Confirm(CredentialMfaAuthenticatorType.OobSms, login, oobCode, ooBConfirmationCode);
            var authenticator = await GetAuthenticator(CredentialMfaAuthenticatorType.TotpAuthenticator, login);

            var result = await Api.DeleteAsync(new DisassociateCredentialMfaAuthenticatorForCallerRequest
            {
                Id = authenticator!.Id
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var authenticators = await GetAuthenticators(login);
            authenticators.Count.Should().Be(2);
            authenticators[0].Type.Should().Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
        }

        [Fact]
        public async Task WhenConfirmMfaAuthenticatorAssociationWithOtpAuthenticator_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, login);
            var confirmationCode = _mfaService.GetOtpCodeNow();

            var result = await Api.PutAsync(new ConfirmCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = CredentialMfaAuthenticatorType.TotpAuthenticator,
                    ConfirmationCode = confirmationCode
                },
                req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Tokens.Should().BeNull();
            result.Content.Value.Authenticators!.Count.Should().Be(2);
            result.Content.Value.Authenticators[0].Type.Should()
                .Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            result.Content.Value.Authenticators[0].IsActive.Should().BeTrue();
            result.Content.Value.Authenticators[1].Type.Should()
                .Be(CredentialMfaAuthenticatorType.TotpAuthenticator);
            result.Content.Value.Authenticators[1].IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task WhenConfirmMfaAuthenticatorAssociationWithOobSms_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var (oobCode, confirmationCode, _) = await Associate(CredentialMfaAuthenticatorType.OobSms, login);

            var result = await Api.PutAsync(new ConfirmCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = CredentialMfaAuthenticatorType.OobSms,
                    OobCode = oobCode,
                    ConfirmationCode = confirmationCode
                },
                req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Tokens.Should().BeNull();
            result.Content.Value.Authenticators!.Count.Should().Be(2);
            result.Content.Value.Authenticators[0].Type.Should()
                .Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            result.Content.Value.Authenticators[0].IsActive.Should().BeTrue();
            result.Content.Value.Authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobSms);
            result.Content.Value.Authenticators[1].IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task WhenConfirmMfaAuthenticatorAssociationWithOobEmail_ThenAuthenticates()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            var (oobCode, confirmationCode, _) =
                await Associate(CredentialMfaAuthenticatorType.OobEmail, login);

            var result = await Api.PutAsync(new ConfirmCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = CredentialMfaAuthenticatorType.OobEmail,
                    OobCode = oobCode,
                    ConfirmationCode = confirmationCode
                },
                req => req.SetJWTBearerToken(login.AccessToken));

            result.Content.Value.Tokens.Should().BeNull();
            result.Content.Value.Authenticators!.Count.Should().Be(2);
            result.Content.Value.Authenticators[0].Type.Should()
                .Be(CredentialMfaAuthenticatorType.RecoveryCodes);
            result.Content.Value.Authenticators[0].IsActive.Should().BeTrue();
            result.Content.Value.Authenticators[1].Type.Should().Be(CredentialMfaAuthenticatorType.OobEmail);
            result.Content.Value.Authenticators[1].IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task WhenResetUserMfaByOperator_ThenResetsMfaToDefault()
        {
            var login = await LoginUserAsync();
            await EnableMfa(login);
            await Associate(CredentialMfaAuthenticatorType.TotpAuthenticator, login);
            var confirmationCode = _mfaService.GetOtpCodeNow();
            await Confirm(CredentialMfaAuthenticatorType.TotpAuthenticator, login, null, confirmationCode);

            var @operator = await LoginUserAsync(LoginUser.Operator);
            var result = await Api.PutAsync(new ResetCredentialMfaRequest
            {
                UserId = login.User.Id
            }, req => req.SetJWTBearerToken(@operator.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Accepted);

            await EnableMfa(login);
            var authenticators = await GetAuthenticators(login);

            authenticators.Count.Should().Be(0);
        }

        private async Task<CredentialMfaAuthenticator?> GetAuthenticator(
            CredentialMfaAuthenticatorType type, LoginDetails login)
        {
            var authenticators = await GetAuthenticators(login);

            return authenticators
                .FirstOrDefault(auth => auth.Type == type);
        }

        private async Task<(string OobCode, string ConfirmationCode, List<string>? recoveryCodes)> Associate(
            CredentialMfaAuthenticatorType type, LoginDetails login, string phoneNumber = "+6498876986")
        {
            var associated = await Api.PostAsync(new AssociateCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = type,
                    PhoneNumber = type == CredentialMfaAuthenticatorType.OobSms
                        ? phoneNumber
                        : null
                },
                req => req.SetJWTBearerToken(login.AccessToken));

            var oobCode = associated.Content.Value.Authenticator.OobCode!;
            var confirmationCode = _mfaService.LastOobConfirmationCode!;
            var recoveryCodes = associated.Content.Value.Authenticator.RecoveryCodes;

            return (oobCode, confirmationCode, recoveryCodes);
        }

        private async Task Confirm(CredentialMfaAuthenticatorType type, LoginDetails login,
            string? oobCode = null, string? confirmationCode = null)
        {
            await Api.PutAsync(new ConfirmCredentialMfaAuthenticatorForCallerRequest
                {
                    AuthenticatorType = type,
                    OobCode = oobCode,
                    ConfirmationCode = confirmationCode
                },
                req => req.SetJWTBearerToken(login.AccessToken));
        }

        private async Task<List<CredentialMfaAuthenticator>> GetAuthenticators(LoginDetails login)
        {
            var authenticators = await Api.GetAsync(new ListCredentialMfaAuthenticatorsForCallerRequest(),
                req => req.SetJWTBearerToken(login.AccessToken));

            return authenticators.Content.Value.Authenticators;
        }

        private async Task EnableMfa(LoginDetails login)
        {
            await Api.PutAsync(new ChangeCredentialMfaForCallerRequest
            {
                IsEnabled = true
            }, req => req.SetJWTBearerToken(login.AccessToken));
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
            services.AddSingleton<IMfaService>(c =>
                new StubMfaService(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                    c.GetRequiredService<ITokensService>()));
        }
    }
}