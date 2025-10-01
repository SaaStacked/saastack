using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PersonCredentials;

public class
    ResendPersonCredentialRegistrationConfirmationRequestValidator : AbstractValidator<
    ResendPersonCredentialRegistrationConfirmationRequest>
{
    public ResendPersonCredentialRegistrationConfirmationRequestValidator()
    {
        RuleFor(req => req.Token)
            .NotEmpty()
            .Matches(Validations.Credentials.Password.VerificationToken)
            .WithMessage(Resources.ConfirmPersonCredentialRegistrationRequestValidator_InvalidToken);
    }
}