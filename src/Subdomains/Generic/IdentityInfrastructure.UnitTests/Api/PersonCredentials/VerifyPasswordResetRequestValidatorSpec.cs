using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.PersonCredentials;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.PersonCredentials;

[Trait("Category", "Unit")]
public class VerifyPasswordResetRequestValidatorSpec
{
    private readonly VerifyPasswordResetRequest _dto;
    private readonly VerifyPasswordResetRequestValidator _validator;

    public VerifyPasswordResetRequestValidatorSpec()
    {
        _validator = new VerifyPasswordResetRequestValidator();
        _dto = new VerifyPasswordResetRequest
        {
            Token = new TokensService().CreatePasswordResetToken()
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
            .WithMessageLike(Resources.CompletePasswordResetRequestValidator_InvalidToken);
    }

    [Fact]
    public void WhenTokenIsInvalid_ThenThrows()
    {
        _dto.Token = "notavalidtoken";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.CompletePasswordResetRequestValidator_InvalidToken);
    }
}