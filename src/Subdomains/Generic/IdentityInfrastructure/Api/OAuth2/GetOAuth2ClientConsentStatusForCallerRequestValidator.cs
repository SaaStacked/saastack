using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class
    GetOAuth2ClientConsentStatusForCallerRequestValidator : AbstractValidator<
    GetOAuth2ClientConsentStatusForCallerRequest>
{
    public GetOAuth2ClientConsentStatusForCallerRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);

        RuleFor(req => req.Scope)
            .NotEmpty()
            .Matches(Validations.OpenIdConnect.Scope)
            .WithMessage(Resources.GetOAuth2ClientConsentStatusForCallerRequestValidator_InvalidScope);
    }
}