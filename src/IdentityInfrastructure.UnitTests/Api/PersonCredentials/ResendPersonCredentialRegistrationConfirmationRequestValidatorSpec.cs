using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.PersonCredentials;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.PersonCredentials;

[Trait("Category", "Unit")]
public class ResendPersonCredentialRegistrationConfirmationRequestValidatorSpec
{
    private readonly ResendPersonCredentialRegistrationConfirmationRequest _dto;
    private readonly ResendPersonCredentialRegistrationConfirmationRequestValidator _validator;

    public ResendPersonCredentialRegistrationConfirmationRequestValidatorSpec()
    {
        _validator = new ResendPersonCredentialRegistrationConfirmationRequestValidator();
        _dto = new ResendPersonCredentialRegistrationConfirmationRequest
        {
            Token = new TokensService().CreateRegistrationVerificationToken()
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenTokenIsEmpty_ThenThrows()
    {
        _dto.Token = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConfirmPersonCredentialRegistrationRequestValidator_InvalidToken);
    }

    [Fact]
    public void WhenTokenIsInvalid_ThenThrows()
    {
        _dto.Token = "aninvalidtoken";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConfirmPersonCredentialRegistrationRequestValidator_InvalidToken);
    }
}