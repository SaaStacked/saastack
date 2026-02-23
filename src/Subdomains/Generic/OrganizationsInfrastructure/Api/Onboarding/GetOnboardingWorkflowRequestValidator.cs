using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

namespace OrganizationsInfrastructure.Api.Onboarding;

public class GetOnboardingWorkflowRequestValidator : AbstractValidator<GetOnboardingWorkflowRequest>
{
    public GetOnboardingWorkflowRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}