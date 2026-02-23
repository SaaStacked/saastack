using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using OrganizationsApplication.ApplicationServices;
using OrganizationsApplication.Persistence;
using OrganizationsApplication.Persistence.ReadModels;
using OrganizationsDomain;
using OrganizationsDomain.DomainServices;
using OrganizationsInfrastructure.DomainServices;

namespace OrganizationsInfrastructure.ApplicationServices;

/// <summary>
///     Provides a service for managing custom onboarding workflows
/// </summary>
public class CustomOnboardingWorkflowService : ICustomOnboardingWorkflowService
{
    private readonly IOnboardingCustomWorkflowRepository _repository;
    private readonly IOnboardingWorkflowService _workflowService;

    public CustomOnboardingWorkflowService(IOnboardingCustomWorkflowRepository repository)
    {
        _repository = repository;
        _workflowService = new OnboardingWorkflowGraphService(this);
    }

    public async Task<Result<WorkflowSchema, Error>> FindWorkflowAsync(string organizationId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        return retrieved.Value.FromWorkflowSchema(_workflowService);
    }

    public async Task<Result<OrganizationOnboardingWorkflowSchema, Error>> SaveWorkflowAsync(string organizationId,
        OrganizationOnboardingWorkflowSchema workflow, CancellationToken cancellationToken)
    {
        var readModel = workflow.ToCustomWorkflow(organizationId.ToId());
        if (readModel.IsFailure)
        {
            return readModel.Error;
        }

        var customWorkflow = readModel.Value;
        var saved = await _repository.SaveAsync(customWorkflow, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return workflow;
    }
}

internal static class CustomOnboardingWorkflowConversionExtensions
{
    public static WorkflowSchema FromWorkflowSchema(this OnboardingCustomWorkflow customWorkflow,
        IOnboardingWorkflowService workflowService)
    {
        var dto = new OrganizationOnboardingWorkflowSchema
        {
            Name = customWorkflow.Name,
            StartStepId = customWorkflow.StartStepId,
            EndStepId = customWorkflow.EndStepId,
            Steps = customWorkflow.Journey.FromJourney()
        };

        return dto.ToWorkflowSchema(workflowService).Value;
    }

    public static Result<OnboardingCustomWorkflow, Error> ToCustomWorkflow(
        this OrganizationOnboardingWorkflowSchema schema,
        Identifier organizationId)
    {
        var journey = schema.Steps.ToJourney();
        if (journey.IsFailure)
        {
            return journey.Error;
        }

        return new OnboardingCustomWorkflow
        {
            Id = organizationId.ToString(),
            Name = schema.Name,
            StartStepId = schema.StartStepId,
            EndStepId = schema.EndStepId,
            Journey = journey.Value
        };
    }

    private static Result<JourneySchema, Error> ToJourney(
        this Dictionary<string, OrganizationOnboardingStepSchema> steps)
    {
        var stepSchemas = new Dictionary<string, StepSchema>();
        foreach (var kvp in steps)
        {
            var step = kvp.Value.ToStepSchema();
            if (step.IsFailure)
            {
                return step.Error;
            }

            stepSchemas[kvp.Key] = step.Value;
        }

        return JourneySchema.Create(stepSchemas);
    }

    private static Dictionary<string, OrganizationOnboardingStepSchema> FromJourney(
        this Optional<JourneySchema> journey)
    {
        return journey.Value.Steps.ToDictionary(step => step.Key, step =>
        {
            return new OrganizationOnboardingStepSchema
            {
                Id = step.Value.Id,
                Type = step.Value.Type.ToEnumOrDefault(OrganizationOnboardingStepSchemaType.Start),
                Title = step.Value.Title,
                Description = step.Value.Description.ValueOrDefault,
                NextStepId = step.Value.NextStepId.ValueOrDefault,
                Branches = step.Value.Branches.Items.Select(branch => new OrganizationOnboardingBranchSchema
                {
                    Id = branch.Id,
                    Label = branch.Label,
                    NextStepId = branch.NextStepId,
                    Condition = new OrganizationOnboardingBranchConditionSchema
                    {
                        Operator = branch.Condition.Operator.ToEnumOrDefault(
                            OrganizationOnboardingBranchConditionSchemaOperator
                                .Equals),
                        Field = branch.Condition.Field,
                        Value = branch.Condition.Value
                    }
                }).ToList(),
                Weight = step.Value.Weight,
                InitialValues = step.Value.InitialValues.Items.ToDictionary(value => value.Key, value => value.Value)
            };
        });
    }

    private static Result<WorkflowSchema, Error> ToWorkflowSchema(this OrganizationOnboardingWorkflowSchema schema,
        IOnboardingWorkflowService workflowService)
    {
        var steps = new Dictionary<string, StepSchema>();
        foreach (var kvp in schema.Steps)
        {
            var step = kvp.Value.ToStepSchema();
            if (step.IsFailure)
            {
                return step.Error;
            }

            steps[kvp.Key] = step.Value;
        }

        return WorkflowSchema.Create(workflowService, schema.Name, steps, schema.StartStepId,
            schema.EndStepId);
    }

    private static Result<StepSchema, Error> ToStepSchema(this OrganizationOnboardingStepSchema step)
    {
        var branches = new List<BranchSchema>();
        if (step.Branches.HasAny())
        {
            foreach (var branchDto in step.Branches!)
            {
                var branch = branchDto.ToBranchSchema();
                if (branch.IsFailure)
                {
                    return branch.Error;
                }

                branches.Add(branch.Value);
            }
        }

        return StepSchema.Create(
            step.Id,
            step.Type.ToEnumOrDefault(OnboardingStepType.Start),
            step.Title,
            step.Description.ToOptional(),
            step.NextStepId.ToOptional(),
            branches,
            step.Weight,
            step.InitialValues ?? new Dictionary<string, string>());
    }

    private static Result<BranchSchema, Error> ToBranchSchema(this OrganizationOnboardingBranchSchema branch)
    {
        var condition = branch.Condition.ToConditionSchema();
        if (condition.IsFailure)
        {
            return condition.Error;
        }

        return BranchSchema.Create(branch.Id, branch.Label, condition.Value, branch.NextStepId);
    }

    private static Result<BranchConditionSchema, Error> ToConditionSchema(
        this OrganizationOnboardingBranchConditionSchema condition)
    {
        return BranchConditionSchema.Create(condition.Operator.ToEnumOrDefault(BranchConditionOperator.Equals),
            condition.Field,
            condition.Value);
    }
}