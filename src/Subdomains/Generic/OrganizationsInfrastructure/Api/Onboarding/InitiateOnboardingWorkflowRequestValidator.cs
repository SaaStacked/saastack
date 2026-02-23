using Application.Resources.Shared;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;
using OrganizationsDomain;

namespace OrganizationsInfrastructure.Api.Onboarding;

public class InitiateOnboardingWorkflowRequestValidator : AbstractValidator<InitiateOnboardingWorkflowRequest>
{
    public InitiateOnboardingWorkflowRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.Workflow)
            .NotNull()
            .WithMessage(Resources.InitiateOnboardingWorkflowRequestValidator_InvalidWorkflow);
        RuleFor(req => req.Workflow)
            .SetValidator(new WorkflowSchemaValidator()!)
            .When(req => req.Workflow.Exists());
    }
}

public class WorkflowSchemaValidator : AbstractValidator<OrganizationOnboardingWorkflowSchema>
{
    public WorkflowSchemaValidator()
    {
        RuleFor(wf => wf.Name)
            .NotEmpty()
            .Matches(Validations.Onboarding.Workflow.Name)
            .WithMessage(Resources.WorkflowSchemaValidator_InvalidName);
        RuleFor(wf => wf.StartStepId)
            .NotEmpty()
            .Matches(Validations.Onboarding.Workflow.StepId)
            .WithMessage(Resources.WorkflowSchemaValidator_InvalidStartStepId);
        RuleFor(wf => wf.EndStepId)
            .NotEmpty()
            .Matches(Validations.Onboarding.Workflow.StepId)
            .WithMessage(Resources.WorkflowSchemaValidator_InvalidEndStepId);
        RuleFor(wf => wf.Steps)
            .NotEmpty()
            .WithMessage(Resources.WorkflowSchemaValidator_InvalidSteps);
        RuleForEach(wf => wf.Steps.Values)
            .SetValidator(new StepSchemaValidator())
            .When(wf => wf.Steps.Exists() && wf.Steps.Count > 0);
    }
}

public class StepSchemaValidator : AbstractValidator<OrganizationOnboardingStepSchema>
{
    public StepSchemaValidator()
    {
        RuleFor(step => step.Id)
            .NotEmpty()
            .Matches(Validations.Onboarding.Workflow.StepId)
            .WithMessage(Resources.StepSchemaValidator_InvalidId);
        RuleFor(step => step.Title)
            .NotEmpty()
            .Matches(Validations.Onboarding.Workflow.StepTitle)
            .WithMessage(Resources.StepSchemaValidator_InvalidTitle);
        RuleFor(step => step.Weight)
            .InclusiveBetween(0, 100)
            .WithMessage(Resources.StepSchemaValidator_InvalidWeight);
        RuleFor(step => step.NextStepId)
            .Matches(Validations.Onboarding.Workflow.StepId)
            .When(step => step.NextStepId.HasValue())
            .WithMessage(Resources.StepSchemaValidator_InvalidNextStepId);
        RuleForEach(step => step.Branches)
            .SetValidator(new BranchSchemaValidator())
            .When(step => step.Branches.Exists() && step.Branches.Count > 0);
    }
}

public class BranchSchemaValidator : AbstractValidator<OrganizationOnboardingBranchSchema>
{
    public BranchSchemaValidator()
    {
        RuleFor(branch => branch.Id)
            .NotEmpty()
            .Matches(Validations.Onboarding.Workflow.StepId)
            .WithMessage(Resources.BranchSchemaValidator_InvalidId);
        RuleFor(branch => branch.Label)
            .NotEmpty()
            .Matches(Validations.Onboarding.Workflow.StepTitle)
            .WithMessage(Resources.BranchSchemaValidator_InvalidLabel);
        RuleFor(branch => branch.NextStepId)
            .NotEmpty()
            .Matches(Validations.Onboarding.Workflow.StepId)
            .WithMessage(Resources.BranchSchemaValidator_InvalidNextStepId);
        RuleFor(branch => branch.Condition)
            .NotNull()
            .WithMessage(Resources.BranchSchemaValidator_InvalidCondition);
        RuleFor(branch => branch.Condition)
            .SetValidator(new BranchConditionSchemaValidator())
            .When(branch => branch.Condition.Exists());
    }
}

public class BranchConditionSchemaValidator : AbstractValidator<OrganizationOnboardingBranchConditionSchema>
{
    public BranchConditionSchemaValidator()
    {
        RuleFor(condition => condition.Field)
            .NotEmpty()
            .WithMessage(Resources.BranchConditionSchemaValidator_InvalidField);
        RuleFor(condition => condition.Value)
            .NotEmpty()
            .WithMessage(Resources.BranchConditionSchemaValidator_InvalidValue);
    }
}