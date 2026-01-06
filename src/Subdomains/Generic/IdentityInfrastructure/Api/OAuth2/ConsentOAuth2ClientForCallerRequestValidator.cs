using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class ConsentOAuth2ClientForCallerRequestValidator : AbstractValidator<ConsentOAuth2ClientForCallerRequest>
{
    public ConsentOAuth2ClientForCallerRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);

        RuleFor(req => req.Scope)
            .NotEmpty()
            .Matches(Validations.OpenIdConnect.Scope)
            .WithMessage(Resources.ConsentOAuth2ClientForCallerRequestValidator_InvalidScope);

        RuleFor(req => req.RedirectUri)
            .IsUrl()
            .WithMessage(Resources.ConsentOAuth2ClientForCallerRequestValidator_InvalidRedirectUri);

        RuleFor(req => req.State)
            .Matches(Validations.OAuth2.State)
            .WithMessage(Resources.ConsentOAuth2ClientForCallerRequestValidator_InvalidState)
            .When(req => req.State.HasValue());
    }
}