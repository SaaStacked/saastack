using Domain.Common.Identity;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class GetOAuth2ClientRequestValidatorSpec
{
    private readonly GetOAuth2ClientRequest _dto;
    private readonly GetOAuth2ClientRequestValidator _validator;

    public GetOAuth2ClientRequestValidatorSpec()
    {
        _validator = new GetOAuth2ClientRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new GetOAuth2ClientRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}