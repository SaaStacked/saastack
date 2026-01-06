using Domain.Common.Identity;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class GetOAuth2ClientConsentStatusForCallerRequestValidatorSpec
{
    private readonly GetOAuth2ClientConsentStatusForCallerRequest _dto;
    private readonly GetOAuth2ClientConsentStatusForCallerRequestValidator _validator;

    public GetOAuth2ClientConsentStatusForCallerRequestValidatorSpec()
    {
        _validator = new GetOAuth2ClientConsentStatusForCallerRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new GetOAuth2ClientConsentStatusForCallerRequest
        {
            Id = "anid",
            Scope = OAuth2Constants.Scopes.Email
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenScopeIsNull_ThenThrows()
    {
        _dto.Scope = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.GetOAuth2ClientConsentStatusForCallerRequestValidator_InvalidScope);
    }
}