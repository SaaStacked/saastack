using Common.Extensions;
using Domain.Common.Identity;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using JetBrains.Annotations;

namespace WebsiteHost.Api.OAuth2;

[UsedImplicitly]
public class AuthorizeOAuth2RequestValidator : AbstractValidator<AuthorizeOAuth2Request>
{
    public AuthorizeOAuth2RequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.ClientId)
            .IsEntityId(identifierFactory)
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidClientId)
            .When(req => req.ClientId.HasValue());

        RuleFor(req => req.RedirectUri)
            .IsUrl()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidRedirectUri)
            .When(req => req.RedirectUri.HasValue());

        RuleFor(req => req.ResponseType)
            .IsInEnum()
            .NotNull()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidResponseType)
            .When(req => req.ResponseType.Exists());

        RuleFor(req => req.Scope)
            .NotEmpty()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidScope)
            .When(req => req.Scope.HasValue());

        RuleFor(req => req.State)
            .NotEmpty()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidState)
            .When(req => req.State.HasValue());

        RuleFor(req => req.Nonce)
            .NotEmpty()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidNonce)
            .When(req => req.Nonce.HasValue());

        RuleFor(req => req.CodeChallenge)
            .NotEmpty()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidCodeChallenge)
            .When(req => req.CodeChallenge.HasValue());

        RuleFor(req => req.CodeChallengeMethod)
            .IsInEnum()
            .NotNull()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidCodeChallengeMethod)
            .When(req => req.CodeChallengeMethod.HasValue);
    }
}