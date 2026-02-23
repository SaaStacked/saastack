using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;
using OrganizationsDomain;

namespace OrganizationsInfrastructure.Api.Onboarding;

public class MoveForwardWorkflowStepRequestValidator : AbstractValidator<MoveForwardWorkflowStepRequest>
{
    public MoveForwardWorkflowStepRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);

        RuleFor(req => req.NextStepId)
            .Matches(Validations.Onboarding.Workflow.StepId)
            .When(req => req.NextStepId.HasValue())
            .WithMessage(Resources.MoveToWorkflowStepRequestValidator_InvalidNextStepId);
    }
}