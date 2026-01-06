using Application.Resources.Shared;
using Domain.Common.Identity;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using JetBrains.Annotations;
using WebsiteHost.Api.OAuth2;
using Xunit;

namespace WebsiteHost.UnitTests.Api.OAuth2;

[UsedImplicitly]
public class AuthorizeOAuth2RequestValidatorSpec
{
    [Trait("Category", "Unit")]
    public class GivenAllParameters
    {
        private readonly AuthorizeOAuth2Request _dto;
        private readonly AuthorizeOAuth2RequestValidator _validator;

        public GivenAllParameters()
        {
            _validator = new AuthorizeOAuth2RequestValidator(new FixedIdentifierFactory("anid"));
            _dto = new AuthorizeOAuth2Request
            {
                ClientId = "anid",
                RedirectUri = "https://localhost/callback",
                ResponseType = OAuth2ResponseType.Code,
                Scope = "ascope",
                State = "astate",
                Nonce = "anonce",
                CodeChallenge = "acodechallenge",
                CodeChallengeMethod = OpenIdConnectCodeChallengeMethod.Plain
            };
        }

        [Fact]
        public void WhenAllProperties_ThenSucceeds()
        {
            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenNonceIsNull_ThenSucceeds()
        {
            _dto.Nonce = null;

            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenCodeChallengeIsNull_ThenSucceeds()
        {
            _dto.CodeChallenge = null;

            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenCodeChallengeAndCodeChallengeMethodIsNull_ThenSucceeds()
        {
            _dto.CodeChallenge = null;
            _dto.CodeChallengeMethod = null;

            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenCodeChallengeIsDefinedAndCodeChallengeMethodIsNull_ThenSucceeds()
        {
            _dto.CodeChallenge = "acodechallenge";
            _dto.CodeChallengeMethod = null;

            _validator.ValidateAndThrow(_dto);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenNoParameters
    {
        private readonly AuthorizeOAuth2Request _dto;
        private readonly AuthorizeOAuth2RequestValidator _validator;

        public GivenNoParameters()
        {
            _validator = new AuthorizeOAuth2RequestValidator(new FixedIdentifierFactory("anid"));
            _dto = new AuthorizeOAuth2Request();
        }

        [Fact]
        public void WhenAllProperties_ThenSucceeds()
        {
            _validator.ValidateAndThrow(_dto);
        }
    }
}