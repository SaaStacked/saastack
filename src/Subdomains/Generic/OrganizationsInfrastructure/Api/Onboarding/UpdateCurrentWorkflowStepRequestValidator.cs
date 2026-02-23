using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

namespace OrganizationsInfrastructure.Api.Onboarding;

public class UpdateCurrentWorkflowStepRequestValidator : AbstractValidator<UpdateCurrentWorkflowStepRequest>
{
    public UpdateCurrentWorkflowStepRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);

        RuleFor(req => req.Values)
            .NotEmpty()
            .WithMessage(Resources.UpdateCurrentWorkflowStepRequestValidator_InvalidValues);
    }
}