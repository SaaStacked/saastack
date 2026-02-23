using Application.Resources.Shared;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;
using OrganizationsInfrastructure.Api.Onboarding;
using UnitTesting.Common.Validation;
using Xunit;

namespace OrganizationsInfrastructure.UnitTests.Api.Onboarding;

[Trait("Category", "Unit")]
public class InitiateOnboardingWorkflowRequestValidatorSpec
{
    private readonly InitiateOnboardingWorkflowRequest _dto;
    private readonly InitiateOnboardingWorkflowRequestValidator _validator;

    public InitiateOnboardingWorkflowRequestValidatorSpec()
    {
        _validator = new InitiateOnboardingWorkflowRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new InitiateOnboardingWorkflowRequest
        {
            Id = "anid",
            Workflow = new OrganizationOnboardingWorkflowSchema
            {
                Name = "aname",
                StartStepId = "astartstepid",
                EndStepId = "anendstepid",
                Steps = new Dictionary<string, OrganizationOnboardingStepSchema>
                {
                    {
                        "astartstepid", new OrganizationOnboardingStepSchema
                        {
                            Id = "astartstepid",
                            Type = OrganizationOnboardingStepSchemaType.Start,
                            Title = "Start Step",
                            NextStepId = "anendstepid",
                            Weight = 100
                        }
                    },
                    {
                        "anendstepid", new OrganizationOnboardingStepSchema
                        {
                            Id = "anendstepid",
                            Type = OrganizationOnboardingStepSchemaType.End,
                            Title = "End Step",
                            Weight = 0
                        }
                    }
                }
            }
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenIdIsInvalid_ThenThrows()
    {
        _dto.Id = "aninvalidid^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(CommonValidationResources.AnyValidator_InvalidId);
    }

    [Fact]
    public void WhenWorkflowIsNull_ThenThrows()
    {
        _dto.Workflow = null!;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.InitiateOnboardingWorkflowRequestValidator_InvalidWorkflow);
    }
}

[Trait("Category", "Unit")]
public class WorkflowSchemaValidatorSpec
{
    private readonly OrganizationOnboardingWorkflowSchema _dto;
    private readonly WorkflowSchemaValidator _validator;

    public WorkflowSchemaValidatorSpec()
    {
        _validator = new WorkflowSchemaValidator();
        _dto = new OrganizationOnboardingWorkflowSchema
        {
            Name = "aname",
            StartStepId = "astartstepid",
            EndStepId = "anendstepid",
            Steps = new Dictionary<string, OrganizationOnboardingStepSchema>
            {
                {
                    "astartstepid", new OrganizationOnboardingStepSchema
                    {
                        Id = "astartstepid",
                        Type = OrganizationOnboardingStepSchemaType.Start,
                        Title = "Start Step",
                        NextStepId = "anendstepid",
                        Weight = 100
                    }
                },
                {
                    "anendstepid", new OrganizationOnboardingStepSchema
                    {
                        Id = "anendstepid",
                        Type = OrganizationOnboardingStepSchemaType.End,
                        Title = "End Step",
                        Weight = 0
                    }
                }
            }
        };
    }

    [Fact]
    public void WhenNameIsEmpty_ThenThrows()
    {
        _dto.Name = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.WorkflowSchemaValidator_InvalidName);
    }

    [Fact]
    public void WhenNameIsInvalid_ThenThrows()
    {
        _dto.Name = "aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.WorkflowSchemaValidator_InvalidName);
    }

    [Fact]
    public void WhenStartStepIdIsEmpty_ThenThrows()
    {
        _dto.StartStepId = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.WorkflowSchemaValidator_InvalidStartStepId);
    }

    [Fact]
    public void WhenStartStepIdIsInvalid_ThenThrows()
    {
        _dto.StartStepId = "aninvalidstepid^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.WorkflowSchemaValidator_InvalidStartStepId);
    }

    [Fact]
    public void WhenEndStepIdIsEmpty_ThenThrows()
    {
        _dto.EndStepId = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.WorkflowSchemaValidator_InvalidEndStepId);
    }

    [Fact]
    public void WhenEndStepIdIsInvalid_ThenThrows()
    {
        _dto.EndStepId = "aninvalidstepid^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.WorkflowSchemaValidator_InvalidEndStepId);
    }

    [Fact]
    public void WhenStepsIsEmpty_ThenThrows()
    {
        _dto.Steps = new Dictionary<string, OrganizationOnboardingStepSchema>();

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.WorkflowSchemaValidator_InvalidSteps);
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}

[Trait("Category", "Unit")]
public class StepSchemaValidatorSpec
{
    private readonly OrganizationOnboardingStepSchema _dto;
    private readonly StepSchemaValidator _validator;

    public StepSchemaValidatorSpec()
    {
        _validator = new StepSchemaValidator();
        _dto = new OrganizationOnboardingStepSchema
        {
            Id = "astepid",
            Type = OrganizationOnboardingStepSchemaType.Start,
            Title = "Start Step",
            NextStepId = "anendstepid",
            Weight = 100
        };
    }

    [Fact]
    public void WhenIdIsEmpty_ThenThrows()
    {
        _dto.Id = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.StepSchemaValidator_InvalidId);
    }

    [Fact]
    public void WhenIdIsInvalid_ThenThrows()
    {
        _dto.Id = "aninvalidstepid^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.StepSchemaValidator_InvalidId);
    }

    [Fact]
    public void WhenTitleIsEmpty_ThenThrows()
    {
        _dto.Title = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.StepSchemaValidator_InvalidTitle);
    }

    [Fact]
    public void WhenTitleIsInvalid_ThenThrows()
    {
        _dto.Title = "aninvalidtitle^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.StepSchemaValidator_InvalidTitle);
    }

    [Fact]
    public void WhenWeightIsNegative_ThenThrows()
    {
        _dto.Weight = -1;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.StepSchemaValidator_InvalidWeight);
    }

    [Fact]
    public void WhenWeightIsGreaterThan100_ThenThrows()
    {
        _dto.Weight = 101;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.StepSchemaValidator_InvalidWeight);
    }

    [Fact]
    public void WhenNextStepIdIsInvalid_ThenThrows()
    {
        _dto.NextStepId = "aninvalidstepid^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.StepSchemaValidator_InvalidNextStepId);
    }

    [Fact]
    public void WhenNextStepIdIsNull_ThenSucceeds()
    {
        _dto.NextStepId = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenHasBranches_ThenSucceeds()
    {
        _dto.Branches = new List<OrganizationOnboardingBranchSchema>
        {
            new()
            {
                Id = "abranchid",
                Label = "Branch 1",
                NextStepId = "anendstepid",
                Condition = new OrganizationOnboardingBranchConditionSchema
                {
                    Field = "afield",
                    Operator = OrganizationOnboardingBranchConditionSchemaOperator.Equals,
                    Value = "avalue"
                }
            }
        };

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}

[Trait("Category", "Unit")]
public class BranchSchemaValidatorSpec
{
    private readonly OrganizationOnboardingBranchSchema _dto;
    private readonly BranchSchemaValidator _validator;

    public BranchSchemaValidatorSpec()
    {
        _validator = new BranchSchemaValidator();
        _dto = new OrganizationOnboardingBranchSchema
        {
            Id = "abranchid",
            Label = "Branch 1",
            NextStepId = "anendstepid",
            Condition = new OrganizationOnboardingBranchConditionSchema
            {
                Field = "afield",
                Operator = OrganizationOnboardingBranchConditionSchemaOperator.Equals,
                Value = "avalue"
            }
        };
    }

    [Fact]
    public void WhenIdIsEmpty_ThenThrows()
    {
        _dto.Id = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.BranchSchemaValidator_InvalidId);
    }

    [Fact]
    public void WhenIdIsInvalid_ThenThrows()
    {
        _dto.Id = "aninvalidbranchid^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.BranchSchemaValidator_InvalidId);
    }

    [Fact]
    public void WhenLabelIsEmpty_ThenThrows()
    {
        _dto.Label = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.BranchSchemaValidator_InvalidLabel);
    }

    [Fact]
    public void WhenNextStepIdIsEmpty_ThenThrows()
    {
        _dto.NextStepId = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.BranchSchemaValidator_InvalidNextStepId);
    }

    [Fact]
    public void WhenConditionIsNull_ThenThrows()
    {
        _dto.Condition = null!;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.BranchSchemaValidator_InvalidCondition);
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}

[Trait("Category", "Unit")]
public class BranchConditionSchemaValidatorSpec
{
    private readonly OrganizationOnboardingBranchConditionSchema _dto;
    private readonly BranchConditionSchemaValidator _validator;

    public BranchConditionSchemaValidatorSpec()
    {
        _validator = new BranchConditionSchemaValidator();
        _dto = new OrganizationOnboardingBranchConditionSchema
        {
            Field = "afield",
            Operator = OrganizationOnboardingBranchConditionSchemaOperator.Equals,
            Value = "avalue"
        };
    }

    [Fact]
    public void WhenFieldIsEmpty_ThenThrows()
    {
        _dto.Field = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.BranchConditionSchemaValidator_InvalidField);
    }

    [Fact]
    public void WhenValueIsEmpty_ThenThrows()
    {
        _dto.Value = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.BranchConditionSchemaValidator_InvalidValue);
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}