using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PersonCredentials;

public class ResendPasswordResetRequestValidator : AbstractValidator<ResendPasswordResetRequest>
{
    public ResendPasswordResetRequestValidator()
    {
        RuleFor(req => req.Token)
            .Matches(Validations.Credentials.Password.ResendToken)
            .WithMessage(Resources.ResendPasswordResetRequestValidator_InvalidToken);
    }
}