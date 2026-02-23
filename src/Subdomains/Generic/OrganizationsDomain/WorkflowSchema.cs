using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using JetBrains.Annotations;
using OrganizationsDomain.DomainServices;

namespace OrganizationsDomain;

public sealed class WorkflowSchema : ValueObjectBase<WorkflowSchema>
{
    public static readonly WorkflowSchema Empty = new("empty", JourneySchema.Empty, "", "");

    public static Result<WorkflowSchema, Error> Create(IOnboardingWorkflowService workflowService, string name,
        IReadOnlyDictionary<string, StepSchema> steps, string startStepId, string endStepId)
    {
        if (name.IsInvalidParameter(Validations.Onboarding.Workflow.Name, nameof(name),
                Resources.OnboardingWorkflow_InvalidName, out var error2))
        {
            return error2;
        }

        if (startStepId.IsInvalidParameter(Validations.Onboarding.Workflow.StepId, nameof(startStepId),
                Resources.OnboardingWorkflow_InvalidStartStepId, out var error3))
        {
            return error3;
        }

        if (endStepId.IsInvalidParameter(Validations.Onboarding.Workflow.StepId, nameof(endStepId),
                Resources.OnboardingWorkflow_InvalidEndStepId, out var error4))
        {
            return error4;
        }

        if (steps.Count == 0)
        {
            return Error.Validation(Resources.OnboardingWorkflow_NoSteps);
        }

        if (!steps.ContainsKey(startStepId))
        {
            return Error.Validation(
                Resources.WorkflowSchema_StartStepMissingFromSteps.Format(startStepId));
        }

        if (!steps.ContainsKey(endStepId))
        {
            return Error.Validation(
                Resources.WorkflowSchema_EndStepMissingFromSteps.Format(endStepId));
        }

        if (steps[startStepId].Type != OnboardingStepType.Start)
        {
            return Error.Validation(Resources.WorkflowSchema_StartNodeIncorrectType.Format(OnboardingStepType.Start));
        }

        if (steps[endStepId].Type != OnboardingStepType.End)
        {
            return Error.Validation(Resources.WorkflowSchema_EndNodeIncorrectType.Format(OnboardingStepType.End));
        }

        // Validate the workflow forms a valid DAG using the domain service
        var validation = workflowService.ValidateWorkflow(steps, startStepId, endStepId);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var journey = JourneySchema.Create(steps);
        if (journey.IsFailure)
        {
            return journey.Error;
        }

        return new WorkflowSchema(name, journey.Value, startStepId, endStepId);
    }

    private WorkflowSchema(string name, JourneySchema journeys, string startStepId, string endStepId)
    {
        Name = name;
        Journeys = journeys;
        StartStepId = startStepId;
        EndStepId = endStepId;
    }

    public string EndStepId { get; }

    public JourneySchema Journeys { get; }

    public string Name { get; }

    public string StartStepId { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Name, Journeys, StartStepId, EndStepId];
    }

    [UsedImplicitly]
    public static ValueObjectFactory<WorkflowSchema> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new WorkflowSchema(parts[0],
                JourneySchema.Rehydrate()(parts[1], container),
                parts[2],
                parts[3]);
        };
    }

    [SkipImmutabilityCheck]
    public Result<Step, Error> CreateStateStep(string stepId)
    {
        if (!TryGetStep(stepId, out var stepSchema))
        {
            return Error.RuleViolation(Resources.WorkflowSchema_CreateStateStep_UnknownStep.Format(stepId));
        }

        return stepSchema.ToStep();
    }

    /// <summary>
    ///     Returns the next step in the workflow.
    ///     If the current step is a branch step, then use the current step values to evaluate the branch condition,
    ///     and see which step evaluates (if any), else just use the current step's next step.
    /// </summary>
    [SkipImmutabilityCheck]
    public Result<string, Error> DetermineNextStep(string currentStepId, StringNameValues currentStepValues)
    {
        if (!Journeys.TryGetValue(currentStepId, out var currentStep))
        {
            return Error.RuleViolation(
                Resources.WorkflowSchema_DetermineNextStep_UnknownCurrentStep.Format(currentStepId));
        }

        if (currentStep.Type == OnboardingStepType.Branch)
        {
            foreach (var branch in currentStep.Branches.Items)
            {
                if (branch.Condition.Evaluate(currentStepValues.Items))
                {
                    return branch.NextStepId;
                }
            }

            // Fall back to the first branch if none of the conditions are met
            return currentStep.Branches.Items.First().NextStepId;
        }

        if (currentStep.NextStepId.HasValue)
        {
            return currentStep.NextStepId.Value;
        }

        // End step has no next
        if (currentStep.Type == OnboardingStepType.End)
        {
            return Error.RuleViolation(Resources.WorkflowSchema_DetermineNextStep_EndStep);
        }

        return Error.RuleViolation(Resources.WorkflowSchema_DetermineNextStep_NoNextStep.Format(currentStepId));
    }

    [SkipImmutabilityCheck]
    public Result<CurrentStepState, Error> InitiateStart(IOnboardingWorkflowService workflowService)
    {
        var allSteps = Journeys.Steps.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var pathAhead = workflowService.CalculateShortestPath(allSteps, StartStepId, EndStepId)
            .TruncateFirstStep().Value;
        var totalWeight = allSteps.Sum(s => s.Value.Weight);
        var initialValues = Journeys.Steps[StartStepId].InitialValues;

        return CurrentStepState.Create(
            OnboardingStatus.InProgress,
            StartStepId,
            Journey.Empty,
            pathAhead,
            totalWeight,
            0,
            initialValues,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None);
    }

    [SkipImmutabilityCheck]
    // ReSharper disable once OutParameterValueIsAlwaysDiscarded.Global
    public bool TryGetStep(string stepId, [NotNullWhen(true)] out StepSchema? step)
    {
        return Journeys.TryGetValue(stepId, out step);
    }

    /// <summary>
    ///     Validates that either the <see cref="toStepId" /> is the next step from <see cref="fromStepId" />,
    ///     or for branch steps, that the <see cref="toStepId" /> is one of the branch steps.
    /// </summary>
    [SkipImmutabilityCheck]
    public Result<Error> ValidateForwardMove(string fromStepId, string toStepId)
    {
        if (!TryGetStep(fromStepId, out var fromStep))
        {
            return Error.RuleViolation(Resources.WorkflowSchema_ValidateMove_UnknownFromStep.Format(fromStepId));
        }

        if (!TryGetStep(toStepId, out _))
        {
            return Error.RuleViolation(Resources.WorkflowSchema_ValidateMove_UnknownToStep.Format(toStepId));
        }

        // For branch steps, make sure toStepId is one of the branch steps
        if (fromStep.Type == OnboardingStepType.Branch)
        {
            var validBranchSteps = fromStep.Branches.Items.Select(step => step.NextStepId).ToList();
            if (!validBranchSteps.Contains(toStepId))
            {
                return Error.RuleViolation(
                    Resources.WorkflowSchema_ValidateMove_InvalidBranchStep.Format(toStepId, fromStepId));
            }
        }

        // FromStepId does not have a next step
        if (!fromStep.NextStepId.HasValue)
        {
            return Result.Ok;
        }

        if (toStepId != fromStep.NextStepId.Value)
        {
            return Error.RuleViolation(
                Resources.WorkflowSchema_ValidateMove_NotDirectlyReachable.Format(toStepId, fromStepId));
        }

        return Result.Ok;
    }
}