using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.PersonCredentials;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Identities;
using FluentAssertions;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class PersonCredentialRootSpec
{
    private const string Token = "5n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";
    private readonly Mock<IEmailAddressService> _emailAddressService;
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IMfaService> _mfaService;
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly PersonCredentialRoot _personCredential;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IConfigurationSettings> _settings;
    private readonly Mock<ITokensService> _tokensService;

    public PersonCredentialRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        var authenticatorCounter = 0;
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity entity) =>
            {
                if (entity is MfaAuthenticator)
                {
                    return $"anauthenticatorid{++authenticatorCounter}".ToId();
                }

                return "anid".ToId();
            });
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _passwordHasherService = new Mock<IPasswordHasherService>();
        _passwordHasherService.Setup(phs => phs.HashPassword(It.IsAny<string>()))
            .Returns("apasswordhash");
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.ValidatePasswordHash(It.IsAny<string>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _mfaService = new Mock<IMfaService>();
        _mfaService.Setup(ms => ms.GenerateOobCode())
            .Returns("anoobcode");
        _mfaService.Setup(ms => ms.GenerateOobSecret())
            .Returns("anoobsecret");
        _mfaService.Setup(
                ms => ms.VerifyTotp(It.IsAny<string>(), It.IsAny<IReadOnlyList<long>>(), It.IsAny<string>()))
            .Returns(TotpResult.Valid(1));
        _mfaService.Setup(ms => ms.GenerateOtpSecret())
            .Returns("anotpsecret");
        _mfaService.Setup(ms => ms.GenerateOtpBarcodeUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("abarcodeuri");
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateRegistrationVerificationToken())
            .Returns("averificationtoken");
        _tokensService.Setup(ts => ts.CreateMfaAuthenticationToken())
            .Returns("anmfatoken");
        _settings = new Mock<IConfigurationSettings>();
        _settings.Setup(s => s.Platform.GetString(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string?)null!);
        _settings.Setup(s => s.Platform.GetNumber(It.IsAny<string>(), It.IsAny<double>()))
            .Returns(5);
        _emailAddressService = new Mock<IEmailAddressService>();
        _emailAddressService.Setup(eas => eas.EnsureUniqueAsync(It.IsAny<EmailAddress>(), It.IsAny<Identifier>()))
            .ReturnsAsync(true);

        _personCredential = PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object,
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId()).Value;
    }

    [Fact]
    public void WhenConstructed_ThenInitialized()
    {
        _personCredential.UserId.Should().Be("auserid".ToId());
        _personCredential.Registration.Should().BeNone();
        _personCredential.Login.IsReset.Should().BeTrue();
        _personCredential.Password.PasswordHash.Should().BeNone();
        _personCredential.MfaOptions.IsEnabled.Should().BeFalse();
        _personCredential.MfaOptions.CanBeDisabled.Should().BeTrue();
        _personCredential.MfaAuthenticators.Should().BeEmpty();
    }

    [Fact]
    public void WhenInitiateRegistrationVerificationAndAlreadyVerified_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = _personCredential.InitiateRegistrationVerification();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationVerified);
    }

    [Fact]
    public void WhenInitiateRegistrationVerificationAndNotVerified_ThenInitiates()
    {
        _personCredential.InitiateRegistrationVerification();

        _personCredential.VerificationKeep.IsStillVerifying.Should().BeTrue();
        _personCredential.Events.Last().Should()
            .BeOfType<RegistrationVerificationCreated>();
    }

    [Fact]
    public void WhenSetCredentialAndInvalidPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(false);

        var result = _personCredential.SetCredentials("notavalidpassword");

        result.Should().BeError(ErrorCode.Validation, Resources.PersonCredentialRoot_InvalidPassword);
    }

    [Fact]
    public void WhenSetCredentials_ThenSetsCredentials()
    {
        _personCredential.SetCredentials("apassword");

        _personCredential.Password.PasswordHash.Should().Be("apasswordhash");
        _personCredential.Events.Last().Should().BeOfType<CredentialsChanged>();
    }

    [Fact]
    public void WhenSetRegistrationDetails_ThenSetsRegistration()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("adisplayname").Value);

        _personCredential.Registration.Value.EmailAddress.Should().Be(EmailAddress.Create("auser@company.com").Value);
        _personCredential.Registration.Value.Name.Should().Be(PersonDisplayName.Create("adisplayname").Value);
        _personCredential.Events.Last().Should().BeOfType<RegistrationChanged>();
    }

    [Fact]
    public void WhenVerifyPasswordWithInvalidPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(false);

        var result = _personCredential.VerifyPassword("1WrongPassword!");

        result.Should().BeError(ErrorCode.Validation, Resources.PersonCredentialRoot_InvalidPassword);
        _personCredential.Login.FailedPasswordAttempts.Should().Be(0);
        _personCredential.Login.IsLocked.Should().BeFalse();
        _personCredential.Login.ToggledLocked.Should().BeFalse();
        _personCredential.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenVerifyPasswordAndWrongPasswordAndAudit_ThenAuditsFailedLogin()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _personCredential.SetCredentials("apassword");
        var result = _personCredential.VerifyPassword("1WrongPassword!");

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        _personCredential.Login.FailedPasswordAttempts.Should().Be(1);
        _personCredential.Login.IsLocked.Should().BeFalse();
        _personCredential.Login.ToggledLocked.Should().BeFalse();
        _personCredential.Events[1].Should().BeOfType<CredentialsChanged>();
        _personCredential.Events.Last().Should().BeOfType<PasswordVerified>();
    }

    [Fact]
    public void WhenVerifyPasswordAndAndAudit_ThenResetsLoginMonitor()
    {
        _personCredential.SetCredentials("apassword");
        var result = _personCredential.VerifyPassword("apassword");

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _personCredential.Login.FailedPasswordAttempts.Should().Be(0);
        _personCredential.Login.IsLocked.Should().BeFalse();
        _personCredential.Login.ToggledLocked.Should().BeFalse();
        _personCredential.Events[1].Should().BeOfType<CredentialsChanged>();
        _personCredential.Events.Last().Should().BeOfType<PasswordVerified>();
        _passwordHasherService.Verify(ph => ph.ValidatePassword("apassword", false));
    }

    [Fact]
    public void WhenVerifyPasswordAndFailsAndLocksAccount_ThenLocksLogin()
    {
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _personCredential.SetCredentials("apassword");
#if TESTINGONLY
        _personCredential.TestingOnly_LockAccount("awrongpassword");
#endif
        _personCredential.Login.FailedPasswordAttempts.Should()
            .Be(Validations.Credentials.Login.DefaultMaxFailedPasswordAttempts);
        _personCredential.Login.IsLocked.Should().BeTrue();
        _personCredential.Login.ToggledLocked.Should().BeTrue();
        _personCredential.Events[1].Should().BeOfType<CredentialsChanged>();
        _personCredential.Events[2].Should().BeOfType<PasswordVerified>();
        _personCredential.Events.Last().Should().BeOfType<AccountLocked>();
    }

    [Fact]
    public void WhenVerifyPasswordAndSucceedsAfterCooldown_ThenUnlocksCredentials()
    {
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _personCredential.SetCredentials("apassword");
#if TESTINGONLY
        _personCredential.TestingOnly_LockAccount("awrongpassword");
        _personCredential.TestingOnly_ResetLoginCooldownPeriod();
#endif
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _personCredential.VerifyPassword("apassword");

        _personCredential.Login.FailedPasswordAttempts.Should().Be(0);
        _personCredential.Login.IsLocked.Should().BeFalse();
        _personCredential.Login.ToggledLocked.Should().BeTrue();
        _personCredential.Events[1].Should().BeOfType<CredentialsChanged>();
        _personCredential.Events[2].Should().BeOfType<PasswordVerified>();
        _personCredential.Events.Last().Should().BeOfType<AccountUnlocked>();
    }

    [Fact]
    public void WhenVerifyRegistrationAndRegistered_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = _personCredential.VerifyRegistration();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationNotVerifying);
    }

    [Fact]
    public void WhenVerifyRegistrationAndExpired_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();
#if TESTINGONLY
        _personCredential.TestingOnly_ExpireRegistrationVerification();
#endif

        var result = _personCredential.VerifyRegistration();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationVerifyingExpired);
    }

    [Fact]
    public void WhenVerifyRegistration_ThenVerified()
    {
        _personCredential.InitiateRegistrationVerification();

        _personCredential.VerifyRegistration();

        _personCredential.VerificationKeep.IsVerified.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<RegistrationVerificationVerified>();
    }

    [Fact]
    public void WhenRenewRegistrationVerificationAndAlreadyVerified_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = _personCredential.RenewRegistrationVerification();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationVerified);
    }

    [Fact]
    public void WhenRenewRegistrationVerificationAndNotVerified_ThenInitiates()
    {
        _personCredential.RenewRegistrationVerification();

        _personCredential.VerificationKeep.IsStillVerifying.Should().BeTrue();
        _personCredential.Events.Last().Should()
            .BeOfType<RegistrationVerificationCreated>();
    }

    [Fact]
    public void WhenInitiatePasswordResetAndPasswordNotSet_ThenReturnsError()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = _personCredential.InitiatePasswordReset();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PasswordKeep_NoPasswordHash);
    }

    [Fact]
    public void WhenInitiatePasswordResetAndNotVerified_ThenReturnsError()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
#if TESTINGONLY
        _personCredential.TestingOnly_RenewVerification(Token);
#endif
        var result = _personCredential.InitiatePasswordReset();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationUnverified);
    }

    [Fact]
    public void WhenInitiatePasswordReset_ThenInitiated()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _personCredential.SetCredentials("apassword");
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        _personCredential.InitiatePasswordReset();

        _personCredential.Password.IsResetInitiated.Should().BeTrue();
        _personCredential.Events[1].Should().BeOfType<CredentialsChanged>();
        _personCredential.Events[2].Should().BeOfType<RegistrationChanged>();
        _personCredential.Events.Last().Should().BeOfType<PasswordResetInitiated>();
    }

    [Fact]
    public void WhenCompletePasswordResetWithInvalidPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(false);

        var result = _personCredential.CompletePasswordReset(Token, "apassword");

        result.Should().BeError(ErrorCode.Validation, Resources.PersonCredentialRoot_InvalidPassword);

        _passwordHasherService.Verify(ph => ph.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void WhenCompletePasswordResetAndNoExistingPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);

        var result = _personCredential.CompletePasswordReset(Token, "apassword");

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialRoot_NoPassword);

        _passwordHasherService.Verify(ph => ph.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void WhenCompletePasswordResetAndSameAsOldPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _personCredential.SetCredentials("apassword");

        var result = _personCredential.CompletePasswordReset(Token, "apassword");

        result.Should().BeError(ErrorCode.Validation, Resources.PersonCredentialRoot_DuplicatePassword);

        _passwordHasherService.Verify(ph => ph.VerifyPassword("apassword", "apasswordhash"));
    }

    [Fact]
    public void WhenCompletePasswordResetAndExpired_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _personCredential.SetCredentials("apassword");
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
#if TESTINGONLY
        _personCredential.TestingOnly_ExpirePasswordResetVerification();
#endif
        var result = _personCredential.CompletePasswordReset("atoken", "apassword");

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_PasswordResetTokenExpired);
    }

    [Fact]
    public void WhenCompletePasswordResetAndCredentialsLocked_ThenResetsPasswordAndUnlocks()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _personCredential.SetCredentials("apassword");
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
#if TESTINGONLY
        _personCredential.TestingOnly_LockAccount("awrongpassword");
#endif
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.InitiatePasswordReset();
        _passwordHasherService.Setup(es => es.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        _personCredential.CompletePasswordReset(_personCredential.Password.ResetToken, "anewpassword");

        _passwordHasherService.Verify(ph => ph.ValidatePassword("apassword", true));
        _passwordHasherService.Verify(ph => ph.ValidatePassword("anewpassword", false));
        _personCredential.Login.IsLocked.Should().BeFalse();
        _personCredential.Login.ToggledLocked.Should().BeTrue();
        _personCredential.Events[12].Should().BeOfType<PasswordResetCompleted>();
        _personCredential.Events.Last().Should().BeOfType<AccountUnlocked>();
    }

    [Fact]
    public void WhenCompletePasswordReset_ThenResetsPassword()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _personCredential.SetCredentials("apassword");
        _personCredential.SetCredentials("apassword");
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.InitiatePasswordReset();

        _personCredential.CompletePasswordReset(_personCredential.Password.ResetToken, "anewpassword");
        _passwordHasherService.Verify(ph => ph.ValidatePassword("apassword", true));
        _passwordHasherService.Verify(ph => ph.ValidatePassword("anewpassword", false));
    }

    [Fact]
    public void WhenEnsureInvariantsAndRegisteredButEmailNotUnique_ThenReturnsErrors()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("adisplayname").Value);
        _emailAddressService.Setup(eas => eas.EnsureUniqueAsync(It.IsAny<EmailAddress>(), It.IsAny<Identifier>()))
            .ReturnsAsync(false);

        var result = _personCredential.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PersonCredentialRoot_EmailNotUnique);
    }

    [Fact]
    public void WhenEnsureInvariantsAndInitiatingPasswordResetButUnRegistered_ThenReturnsErrors()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _personCredential.SetCredentials("apassword");
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.InitiatePasswordReset();

#if TESTINGONLY
        _personCredential.TestingOnly_Unregister();
#endif

        var result = _personCredential.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.PersonCredentialRoot_PasswordInitiatedWithoutRegistration);
    }

    [Fact]
    public void WhenChangeMfaEnabledAndNotOwner_ThenReturnsError()
    {
        var mfaOptions = MfaOptions.Create(false, true).Value;
        var credential = PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object,
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId(), mfaOptions).Value;

        var result =
            credential.ChangeMfaEnabled("amodifierid".ToId(), true);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.PersonCredentialRoot_NotOwner);
    }

    [Fact]
    public void WhenChangeMfaEnabledAndNotVerified_ThenReturnsError()
    {
        var mfaOptions = MfaOptions.Create(true, true).Value;
        var credential = PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object,
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId(), mfaOptions).Value;
        credential.InitiateRegistrationVerification();

        var result =
            credential.ChangeMfaEnabled("auserid".ToId(), true);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationUnverified);
    }

    [Fact]
    public void WhenChangeMfaEnabledAndNoPassword_ThenReturnsError()
    {
        var mfaOptions = MfaOptions.Create(true, true).Value;
        var credential = PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object,
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId(), mfaOptions).Value;
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        credential.InitiateRegistrationVerification();
        credential.VerifyRegistration();

        var result =
            credential.ChangeMfaEnabled("auserid".ToId(), true);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_NoPassword);
    }

    [Fact]
    public void WhenChangeMfaEnabledAndAlreadyEnabled_ThenDoesNothing()
    {
        var mfaOptions = MfaOptions.Create(true, true).Value;
        var credential = PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object,
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId(), mfaOptions).Value;
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        credential.InitiateRegistrationVerification();
        credential.VerifyRegistration();
        credential.SetCredentials("apassword");

        var result =
            credential.ChangeMfaEnabled("auserid".ToId(), true);

        result.Should().BeSuccess();
        credential.MfaOptions.IsEnabled.Should().BeTrue();
        credential.MfaOptions.CanBeDisabled.Should().BeTrue();
        credential.Events.Last().Should().NotBeOfType<MfaOptionsChanged>();
    }

    [Fact]
    public void WhenChangeMfaOptionsWithEnabled_ThenEnables()
    {
        var mfaOptions = MfaOptions.Create(false, true).Value;
        var credential = PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object,
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId(), mfaOptions).Value;
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        credential.InitiateRegistrationVerification();
        credential.VerifyRegistration();
        credential.SetCredentials("apassword");

        var result = credential.ChangeMfaEnabled("auserid".ToId(), true);

        result.Should().BeSuccess();
        credential.MfaOptions.IsEnabled.Should().BeTrue();
        credential.MfaOptions.CanBeDisabled.Should().BeTrue();
        credential.Events.Last().Should().BeOfType<MfaOptionsChanged>();
    }

    [Fact]
    public void WhenChangeMfaOptionsWithDisabled_ThenDisables()
    {
        var mfaOptions = MfaOptions.Create(true, true).Value;
        var credential = PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object,
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId(), mfaOptions).Value;
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        credential.InitiateRegistrationVerification();
        credential.VerifyRegistration();
        credential.SetCredentials("apassword");

        var result = credential.ChangeMfaEnabled("auserid".ToId(), false);

        result.Should().BeSuccess();
        credential.MfaOptions.IsEnabled.Should().BeFalse();
        credential.MfaOptions.CanBeDisabled.Should().BeTrue();
        credential.Events.Last().Should().BeOfType<MfaOptionsChanged>();
    }

    [Fact]
    public async Task
        WhenChangeMfaOptionsWithDisabledAndExistingAuthenticators_ThenDissociatesAuthenticatorsAndDisables()
    {
        var mfaOptions = MfaOptions.Create(true, true).Value;
        var credential = PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object,
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId(), mfaOptions).Value;
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        credential.InitiateRegistrationVerification();
        credential.VerifyRegistration();
        credential.SetCredentials("apassword");
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms,
            PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        credential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, "anoobcode", "anoobsecret");

        credential.MfaAuthenticators.Count.Should().Be(2);

        var result = credential.ChangeMfaEnabled("auserid".ToId(), false);

        result.Should().BeSuccess();
        credential.MfaOptions.IsEnabled.Should().BeFalse();
        credential.MfaOptions.CanBeDisabled.Should().BeTrue();
        credential.MfaAuthenticators.Count.Should().Be(0);
        credential.Events[12].Should().BeOfType<MfaAuthenticatorRemoved>();
        credential.Events[13].Should().BeOfType<MfaAuthenticatorRemoved>();
        credential.Events.Last().Should().BeOfType<MfaOptionsChanged>();
    }

    [Fact]
    public void WhenInitiateMfaAuthenticationAndNotVerified_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();

        var result = _personCredential.InitiateMfaAuthentication();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationUnverified);
    }

    [Fact]
    public void WhenInitiateMfaAuthenticationAndNoPassword_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = _personCredential.InitiateMfaAuthentication();

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialRoot_NoPassword);
    }

    [Fact]
    public void WhenInitiateMfaAuthentication_ThenInitiates()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);

        var result = _personCredential.InitiateMfaAuthentication();

        result.Should().BeSuccess();
        result.Value.Should().Be("anmfatoken");
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticationInitiated>();
    }

    [Fact]
    public void WhenDisassociateMfaAuthenticatorAndIsNotOwner_ThenReturnsError()
    {
        var result = _personCredential.DisassociateMfaAuthenticator(
            MfaCaller.Create("anotheruserid".ToId(), null).Value,
            "anauthenticatorid".ToId());

        result.Should().BeError(ErrorCode.RoleViolation, Resources.PersonCredentialRoot_NotOwner);
    }

    [Fact]
    public void WhenDisassociateMfaAuthenticatorAndIsNotVerified_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();

        var result = _personCredential.DisassociateMfaAuthenticator(MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid".ToId());

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationUnverified);
    }

    [Fact]
    public void WhenDisassociateMfaAuthenticatorAndNoPassword_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = _personCredential.DisassociateMfaAuthenticator(MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid".ToId());

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialRoot_NoPassword);
    }

    [Fact]
    public void WhenDisassociateMfaAuthenticatorAndMfaNotEnabled_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");

        var result = _personCredential.DisassociateMfaAuthenticator(MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PersonCredentialRoot_MfaNotEnabled);
    }

    [Fact]
    public void WhenDisassociateMfaAuthenticatorAndNotExists_ThenReturnsError()
    {
        _tokensService.Setup(ts => ts.CreateMfaAuthenticationToken())
            .Returns("atoken");
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);

        var result = _personCredential.DisassociateMfaAuthenticator(MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid".ToId());

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDisassociateMfaAuthenticatorAndIsRecoveryCodes_ThenReturnsError()
    {
        _tokensService.Setup(ts => ts.CreateMfaAuthenticationToken())
            .Returns("anmfatoken");
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));
        var recoveryCodes = _personCredential.MfaAuthenticators.FindRecoveryCodes();

        var result =
            _personCredential.DisassociateMfaAuthenticator(MfaCaller.Create("auserid".ToId(), null).Value,
                recoveryCodes.Value.Id);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.PersonCredentialRoot_DisassociateMfaAuthenticator_RecoveryCodesCannotBeDeleted);
    }

    [Fact]
    public async Task WhenDisassociateMfaAuthenticatorAndNotLastOne_ThenDeletesAuthenticator()
    {
        _tokensService.Setup(ts => ts.CreateMfaAuthenticationToken())
            .Returns("anmfatoken");
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        var authenticator1 = (await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok))).Value;
        var authenticator2 = (await _personCredential.AssociateMfaAuthenticatorAsync(
                MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
                MfaAuthenticatorType.OobEmail, Optional<PhoneNumber>.None,
                EmailAddress.Create("auser@company.com").Value, Optional<EmailAddress>.None,
                _ => Task.FromResult(Result.Ok)))
            .Value;
        var recoveryCodes = _personCredential.MfaAuthenticators.FindRecoveryCodes();

        var result =
            _personCredential.DisassociateMfaAuthenticator(MfaCaller.Create("auserid".ToId(), null).Value,
                authenticator2.Id);

        result.Should().BeSuccess();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].Id.Should().Be(recoveryCodes.Value.Id);
        _personCredential.MfaAuthenticators[1].Id.Should().Be(authenticator1.Id);
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorRemoved>();
    }

    [Fact]
    public async Task WhenDisassociateMfaAuthenticatorAndLastOne_ThenDeletesAuthenticatorAndRecoveryCodes()
    {
        _tokensService.Setup(ts => ts.CreateMfaAuthenticationToken())
            .Returns("anmfatoken");
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        var authenticator = (await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok))).Value;

        var result =
            _personCredential.DisassociateMfaAuthenticator(MfaCaller.Create("auserid".ToId(), null).Value,
                authenticator.Id);

        result.Should().BeSuccess();
        _personCredential.MfaAuthenticators.Count.Should().Be(0);
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorRemoved>();
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAndIsNotOwner_ThenReturnsError()
    {
        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("anotheruserid".ToId(), "atoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation, Resources.PersonCredentialRoot_NotOwner);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAndIsNotVerified_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationUnverified);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAndNoPassword_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialRoot_NoPassword);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAndMfaNotEnabled_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialRoot_MfaNotEnabled);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAndNotAuthenticated_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.MfaOptions_AuthenticationNotInitiated);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAndTypeIsNone_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.None, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.PersonCredentialRoot_AssociateMfaAuthenticator_InvalidType);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAndTypeIsRecoveryCodes_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.RecoveryCodes, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.PersonCredentialRoot_AssociateMfaAuthenticator_InvalidType);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorByAuthenticatedUserAuthenticatorAlreadyExists_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));
        _personCredential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator,
            Optional<string>.None, "aconfirmationcode");

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_AssociateMfaAuthenticator_AlreadyAssociated);
    }

    [Fact]
    public async Task
        WhenAssociateMfaAuthenticatorByUnauthenticatedUserAndAnotherAuthenticatorIsAlreadyConfirmed_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));
        _personCredential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator,
            Optional<string>.None, "aconfirmationcode");

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_AssociateMfaAuthenticator_MustChallenge);
    }

    [Fact]
    public async Task
        WhenAssociateMfaAuthenticatorByAuthenticatedUserAndAnotherAuthenticatorIsAlreadyAssociated_ThenAssociatesAuthenticator()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        var authenticator1 = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));
        _personCredential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator,
            Optional<string>.None, "aconfirmationcode");

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            MfaAuthenticatorType.OobSms, PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anauthenticatorid3".ToId());
        result.Value.Type.Should().Be(MfaAuthenticatorType.OobSms);
        result.Value.IsActive.Should().BeFalse();
        result.Value.OobChannelValue.Should().BeSome("+6498876986");
        result.Value.OobCode.Should().BeSome("anoobcode");
        result.Value.BarCodeUri.Should().BeNone();
        result.Value.Secret.Should().BeSome("anoobsecret");
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorAssociated>();
        _personCredential.MfaAuthenticators.Count.Should().Be(3);
        _personCredential.MfaAuthenticators[0].Id.Should()
            .Be(_personCredential.MfaAuthenticators.FindRecoveryCodes().Value.Id);
        _personCredential.MfaAuthenticators[1].Id.Should().Be(authenticator1.Value.Id);
        _personCredential.MfaAuthenticators[2].Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForOobSmsButNoPhoneNumber_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.MfaAuthenticator_Associate_OobSms_NoPhoneNumber);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForOobEmailButNoEmailAddress_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.MfaAuthenticator_Associate_OobEmail_NoEmailAddress);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAndFirstAuthenticator_ThenAssociatesAuthenticatorAndRecoveryCodes()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        var wasCalled = false;

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        result.Value.Id.Should().Be("anauthenticatorid2".ToId());
        result.Value.Type.Should().Be(MfaAuthenticatorType.TotpAuthenticator);
        result.Value.IsActive.Should().BeFalse();
        result.Value.OobChannelValue.Should().BeNone();
        result.Value.OobCode.Should().BeNone();
        result.Value.BarCodeUri.Should().BeSome("abarcodeuri");
        result.Value.Secret.Should().BeSome("anotpsecret");
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorAssociated>();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].Id.Should()
            .Be(_personCredential.MfaAuthenticators.FindRecoveryCodes().Value.Id);
        _personCredential.MfaAuthenticators[1].Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAndNextAuthenticator_ThenAssociatesAuthenticator()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        var authenticator1 = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));
        var wasCalled = false;

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        result.Value.Id.Should().Be("anauthenticatorid3".ToId());
        result.Value.Type.Should().Be(MfaAuthenticatorType.OobSms);
        result.Value.IsActive.Should().BeFalse();
        result.Value.OobChannelValue.Should().BeSome("+6498876986");
        result.Value.OobCode.Should().BeSome("anoobcode");
        result.Value.BarCodeUri.Should().BeNone();
        result.Value.Secret.Should().BeSome("anoobsecret");
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorAssociated>();
        _personCredential.MfaAuthenticators.Count.Should().Be(3);
        _personCredential.MfaAuthenticators[0].Id.Should()
            .Be(_personCredential.MfaAuthenticators.FindRecoveryCodes().Value.Id);
        _personCredential.MfaAuthenticators[1].Id.Should().Be(authenticator1.Value.Id);
        _personCredential.MfaAuthenticators[2].Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForTotp_ThenAssociatesAuthenticator()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        var wasCalled = false;

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        result.Value.Id.Should().Be("anauthenticatorid2".ToId());
        result.Value.Type.Should().Be(MfaAuthenticatorType.TotpAuthenticator);
        result.Value.IsActive.Should().BeFalse();
        result.Value.OobChannelValue.Should().BeNone();
        result.Value.OobCode.Should().BeNone();
        result.Value.BarCodeUri.Should().BeSome("abarcodeuri");
        result.Value.Secret.Should().BeSome("anotpsecret");
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorAssociated>();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].Id.Should()
            .Be(_personCredential.MfaAuthenticators.FindRecoveryCodes().Value.Id);
        _personCredential.MfaAuthenticators[1].Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForOobSms_ThenAssociatesAuthenticator()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        var wasCalled = false;

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        result.Value.Id.Should().Be("anauthenticatorid2".ToId());
        result.Value.Type.Should().Be(MfaAuthenticatorType.OobSms);
        result.Value.IsActive.Should().BeFalse();
        result.Value.OobChannelValue.Should().BeSome("+6498876986");
        result.Value.OobCode.Should().BeSome("anoobcode");
        result.Value.BarCodeUri.Should().BeNone();
        result.Value.Secret.Should().BeSome("anoobsecret");
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorAssociated>();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].Id.Should()
            .Be(_personCredential.MfaAuthenticators.FindRecoveryCodes().Value.Id);
        _personCredential.MfaAuthenticators[1].Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorForOobEmail_ThenAssociatesAuthenticator()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        var wasCalled = false;

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail, Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value,
            Optional<EmailAddress>.None,
            _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        result.Value.Id.Should().Be("anauthenticatorid2".ToId());
        result.Value.Type.Should().Be(MfaAuthenticatorType.OobEmail);
        result.Value.IsActive.Should().BeFalse();
        result.Value.OobChannelValue.Should().BeSome("auser@company.com");
        result.Value.OobCode.Should().BeSome("anoobcode");
        result.Value.BarCodeUri.Should().BeNone();
        result.Value.Secret.Should().BeSome("anoobsecret");
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorAssociated>();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].Id.Should()
            .Be(_personCredential.MfaAuthenticators.FindRecoveryCodes().Value.Id);
        _personCredential.MfaAuthenticators[1].Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task WhenReAssociateMfaAuthenticatorThenUpdatesAssociation()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms,
            PhoneNumber.Create("+6498876981").Value, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));

        var result = await _personCredential.AssociateMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, PhoneNumber.Create("+6498876982").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, _ => Task.FromResult(Result.Ok));

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anauthenticatorid2".ToId());
        result.Value.Type.Should().Be(MfaAuthenticatorType.OobSms);
        result.Value.IsActive.Should().BeFalse();
        result.Value.OobChannelValue.Should().BeSome("+6498876982");
        result.Value.OobCode.Should().BeSome("anoobcode");
        result.Value.BarCodeUri.Should().BeNone();
        result.Value.Secret.Should().BeSome("anoobsecret");
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorAssociated>();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].Id.Should()
            .Be(_personCredential.MfaAuthenticators.FindRecoveryCodes().Value.Id);
        _personCredential.MfaAuthenticators[1].Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public void WhenConfirmMfaAuthenticatorAssociationAndNotOwner_ThenReturnsError()
    {
        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("anotheruserid".ToId(), null).Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.PersonCredentialRoot_NotOwner);
    }

    [Fact]
    public void WhenConfirmMfaAuthenticatorAssociationAndIsNotVerified_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationUnverified);
    }

    [Fact]
    public void WhenConfirmMfaAuthenticatorAssociationAndNoPassword_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_NoPassword);
    }

    [Fact]
    public void WhenConfirmMfaAuthenticatorAssociationAndMfaNotEnabled_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.PersonCredentialRoot_MfaNotEnabled);
    }

    [Fact]
    public void WhenConfirmMfaAuthenticatorAssociationAndNotAuthenticated_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.MfaOptions_AuthenticationNotInitiated);
    }

    [Fact]
    public void WhenConfirmMfaAuthenticatorAssociationForNoneAuthenticator_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.None, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.PersonCredentialRoot_AssociateMfaAuthenticator_InvalidType);
    }

    [Fact]
    public void WhenConfirmMfaAuthenticatorAssociationForRecoveryCodesAuthenticator_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.RecoveryCodes, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.PersonCredentialRoot_AssociateMfaAuthenticator_InvalidType);
    }

    [Fact]
    public void WhenConfirmMfaAuthenticatorAssociationAndUnknownAuthenticator_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_CompleteMfaAuthenticatorAssociation_NotFound);
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationForOobSms_ThenConfirms()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms,
            PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, "anoobcode", "anoobsecret");

        result.Should().BeSuccess();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaAuthenticators[1].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorConfirmed>();
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationForOobEmail_ThenConfirms()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail,
            Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail, "anoobcode", "anoobsecret");

        result.Should().BeSuccess();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaAuthenticators[1].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorConfirmed>();
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationForTotpAuthenticator_ThenConfirms()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));

        var result = _personCredential.ConfirmMfaAuthenticatorAssociation(
            MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, "aconfirmationcode");

        result.Should().BeSuccess();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaAuthenticators[1].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorConfirmed>();
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorAsyncAndNotOwner_ThenReturnsError()
    {
        var result = await _personCredential.ChallengeMfaAuthenticatorAsync(
            MfaCaller.Create("anotheruserid".ToId(), "anmfatoken").Value,
            "anauthenticatorid".ToId(), _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation, Resources.PersonCredentialRoot_NotOwner);
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorAsyncAndIsNotVerified_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();

        var result = await _personCredential.ChallengeMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid".ToId(), _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationUnverified);
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorAsyncAndNoPassword_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = await _personCredential.ChallengeMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid".ToId(), _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_NoPassword);
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorAsyncAndMfaNotEnabled_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");

        var result = await _personCredential.ChallengeMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid".ToId(), _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialRoot_MfaNotEnabled);
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorAsyncAndUnknownAuthenticator_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);

        var result = await _personCredential.ChallengeMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid".ToId(), _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorAsyncForOobSms_ThenChallenges()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms,
            PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        _personCredential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, "anoobcode", "anoobsecret");
        var wasCalled = false;

        var result = await _personCredential.ChallengeMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid2".ToId(), _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaAuthenticators[1].IsChallenged.Should().BeTrue();
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorChallenged>();
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorAsyncForOobEmail_ThenChallenges()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail,
            Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        _personCredential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail, "anoobcode", "anoobsecret");
        var wasCalled = false;

        var result = await _personCredential.ChallengeMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid2".ToId(), _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaAuthenticators[1].IsChallenged.Should().BeTrue();
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorChallenged>();
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorAsyncForTotpAuthenticator_ThenConfirmed()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));
        _personCredential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, "aconfirmationcode");
        var wasCalled = false;

        var result = await _personCredential.ChallengeMfaAuthenticatorAsync(
            MfaCaller.Create("auserid".ToId(), null).Value,
            "anauthenticatorid2".ToId(), _ =>
            {
                wasCalled = true;
                return Task.FromResult(Result.Ok);
            });

        result.Should().BeSuccess();
        wasCalled.Should().BeTrue();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaAuthenticators[1].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorConfirmed>();
    }

    [Fact]
    public void WhenVerifyAuthenticatorByAuthenticatedUser_ThenReturnsError()
    {
        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("anotheruserid".ToId(), null).Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
    }

    [Fact]
    public void WhenVerifyAuthenticatorAndNotOwner_ThenReturnsError()
    {
        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("anotheruserid".ToId(), "atoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.PersonCredentialRoot_NotOwner);
    }

    [Fact]
    public void WhenVerifyMfaAuthenticatorAndIsNotVerified_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();

        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationUnverified);
    }

    [Fact]
    public void WhenVerifyMfaAuthenticatorAndNoPassword_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_NoPassword);
    }

    [Fact]
    public void WhenVerifyMfaAuthenticatorAndMfaNotEnabled_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");

        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialRoot_MfaNotEnabled);
    }

    [Fact]
    public void WhenVerifyMfaAuthenticatorAndNotAuthenticated_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);

        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.MfaOptions_AuthenticationNotInitiated);
    }

    [Fact]
    public void WhenVerifyMfaAuthenticatorForNoneAuthenticator_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.None, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.PersonCredentialRoot_AssociateMfaAuthenticator_InvalidType);
    }

    [Fact]
    public void WhenVerifyMfaAuthenticatorAndUnknownAuthenticator_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_CompleteMfaAuthenticatorAssociation_NotFound);
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForOobSms_ThenAuthenticates()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms,
            PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        _personCredential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, "anoobcode", "anoobsecret");

        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, "anoobcode", "anoobsecret");

        result.Should().BeSuccess();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaAuthenticators[1].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorVerified>();
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForOobEmail_ThenAuthenticates()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail,
            Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        _personCredential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail, "anoobcode", "anoobsecret");

        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobEmail, "anoobcode", "anoobsecret");

        result.Should().BeSuccess();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaAuthenticators[1].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorVerified>();
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorForTotpAuthenticator_ThenAuthenticates()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();
        await _personCredential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, _ => Task.FromResult(Result.Ok));
        _personCredential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, "aconfirmationcode");

        var result = _personCredential.VerifyMfaAuthenticator(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.TotpAuthenticator, Optional<string>.None, "aconfirmationcode");

        result.Should().BeSuccess();
        _personCredential.MfaAuthenticators.Count.Should().Be(2);
        _personCredential.MfaAuthenticators[0].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaAuthenticators[1].HasBeenConfirmed.Should().BeTrue();
        _personCredential.MfaOptions.IsAuthenticationInitiated.Should().BeTrue();
        _personCredential.Events.Last().Should().BeOfType<MfaAuthenticatorVerified>();
    }

    [Fact]
    public void WhenResetMfaByOtherUser_ThenReturnsError()
    {
        var result = _personCredential.ResetMfa(Roles.Create("anotherrole").Value);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.PersonCredentialRoot_NotOperator);
    }

    [Fact]
    public void WhenResetMfaByOperatorAndNoPassword_ThenDoesNothing()
    {
        var result = _personCredential.ResetMfa(Roles.Create(PlatformRoles.Operations).Value);

        result.Should().BeSuccess();
        _personCredential.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public async Task WhenResetMfaByOperatorAndExistingAuthenticators_ThenDisassociatesAuthenticatorsAndResets()
    {
        var mfaOptions = MfaOptions.Create(true, true).Value;
        var credential = PersonCredentialRoot.Create(_recorder.Object, _idFactory.Object,
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, "auserid".ToId(), mfaOptions).Value;
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        credential.InitiateRegistrationVerification();
        credential.VerifyRegistration();
        credential.SetCredentials("apassword");
        credential.InitiateMfaAuthentication();
        await credential.AssociateMfaAuthenticatorAsync(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms,
            PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            _ => Task.FromResult(Result.Ok));
        credential.ConfirmMfaAuthenticatorAssociation(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value,
            MfaAuthenticatorType.OobSms, "anoobcode", "anoobsecret");

        credential.MfaAuthenticators.Count.Should().Be(2);

        var result = credential.ResetMfa(Roles.Create(PlatformRoles.Operations).Value);

        result.Should().BeSuccess();
        credential.MfaOptions.IsEnabled.Should().BeFalse();
        credential.MfaOptions.CanBeDisabled.Should().BeTrue();
        credential.MfaAuthenticators.Count.Should().Be(0);
        credential.Events[12].Should().BeOfType<MfaAuthenticatorRemoved>();
        credential.Events[13].Should().BeOfType<MfaAuthenticatorRemoved>();
        credential.Events.Last().Should().BeOfType<MfaStateReset>();
    }

    [Fact]
    public void WhenViewMfaAuthenticatorsAndNotOwner_ThenReturnsError()
    {
        var result = _personCredential.ViewMfaAuthenticators(MfaCaller.Create("anotheruserid".ToId(), "atoken").Value);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.PersonCredentialRoot_NotOwner);
    }

    [Fact]
    public void WhenViewMfaAuthenticatorsAndIsNotVerified_ThenReturnsError()
    {
        _personCredential.InitiateRegistrationVerification();

        var result = _personCredential.ViewMfaAuthenticators(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_RegistrationUnverified);
    }

    [Fact]
    public void WhenViewMfaAuthenticatorsAndNoPassword_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();

        var result = _personCredential.ViewMfaAuthenticators(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PersonCredentialRoot_NoPassword);
    }

    [Fact]
    public void WhenViewMfaAuthenticatorsAndMfaNotEnabled_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");

        var result = _personCredential.ViewMfaAuthenticators(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PersonCredentialRoot_MfaNotEnabled);
    }

    [Fact]
    public void WhenViewMfaAuthenticatorsAndNotAuthenticated_ThenReturnsError()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);

        var result = _personCredential.ViewMfaAuthenticators(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.MfaOptions_AuthenticationNotInitiated);
    }

    [Fact]
    public void WhenViewMfaAuthenticatorsAndAuthenticated_ThenReturns()
    {
        _personCredential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _personCredential.InitiateRegistrationVerification();
        _personCredential.VerifyRegistration();
        _personCredential.SetCredentials("apassword");
        _personCredential.ChangeMfaEnabled("auserid".ToId(), true);
        _personCredential.InitiateMfaAuthentication();

        var result = _personCredential.ViewMfaAuthenticators(MfaCaller.Create("auserid".ToId(), "anmfatoken").Value);

        result.Should().BeSuccess();
    }
}