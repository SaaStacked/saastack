using Common.Extensions;
using Domain.Common.Identity;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

namespace WebsiteHost.Api.OAuth2;

public class ConsentOAuth2ClientRequestValidator : AbstractValidator<ConsentOAuth2ClientRequest>
{
    public ConsentOAuth2ClientRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidClientId);

        RuleFor(req => req.RedirectUri)
            .IsUrl()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidRedirectUri);

        RuleFor(req => req.Scope)
            .NotEmpty()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidScope);

        RuleFor(req => req.State)
            .NotEmpty()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidState)
            .When(req => req.State.HasValue());
    }
}