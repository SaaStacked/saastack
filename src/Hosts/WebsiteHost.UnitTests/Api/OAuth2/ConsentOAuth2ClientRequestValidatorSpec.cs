using Domain.Common.Identity;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using UnitTesting.Common.Validation;
using WebsiteHost.Api.OAuth2;
using Xunit;

namespace WebsiteHost.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class ConsentOAuth2ClientRequestValidatorSpec
{
    private readonly ConsentOAuth2ClientRequest _dto;
    private readonly ConsentOAuth2ClientRequestValidator _validator;

    public ConsentOAuth2ClientRequestValidatorSpec()
    {
        _validator = new ConsentOAuth2ClientRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new ConsentOAuth2ClientRequest
        {
            Id = "anid",
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}, {OAuth2Constants.Scopes.Profile}",
            Consented = true,
            RedirectUri = "https://localhost/callback",
            State = "astate"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenRedirectUriIsNull_ThenThrows()
    {
        _dto.RedirectUri = null;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenScopeIsNull_ThenThrows()
    {
        _dto.Scope = null;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenStateIsNull_ThenSucceeds()
    {
        _dto.State = null;

        _validator.ValidateAndThrow(_dto);
    }
}