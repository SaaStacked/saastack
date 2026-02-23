using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using JetBrains.Annotations;
using OrganizationsDomain.DomainServices;
using OrganizationsDomain.Extensions;
using DictionaryExtensions = OrganizationsDomain.Extensions.DictionaryExtensions;

namespace OrganizationsDomain;

public sealed class CurrentStepState : ValueObjectBase<CurrentStepState>
{
    public static readonly CurrentStepState Empty = new(OnboardingStatus.NotStarted, "", Journey.Empty, Journey.Empty,
        0, 0, 0,
        StringNameValues.Empty, Optional<DateTime>.None, Optional<DateTime>.None, DateTime.UtcNow,
        Optional<string>.None);

    public static Result<CurrentStepState, Error> Create(
        OnboardingStatus status,
        string currentStepId,
        Journey pathTaken,
        Journey pathAhead,
        int totalWeight,
        int completedWeight,
        StringNameValues values,
        Optional<DateTime> startedAt,
        Optional<DateTime> completedAt,
        Optional<string> completedBy)
    {
        if (currentStepId.IsInvalidParameter(id => id.HasValue(), nameof(currentStepId),
                Resources.OnboardingState_InvalidCurrentStepId,
                out var error2))
        {
            return error2;
        }

        if (totalWeight.IsInvalidParameter(tw => tw >= 0, nameof(totalWeight),
                Resources.OnboardingState_InvalidTotalWeight, out var error3))
        {
            return error3;
        }

        if (completedWeight.IsInvalidParameter(cw => cw >= 0 && cw <= totalWeight,
                nameof(completedWeight), Resources.OnboardingState_InvalidCompletedWeight.Format(completedWeight),
                out var error4))
        {
            return error4;
        }

        if (status == OnboardingStatus.Complete && !completedAt.HasValue)
        {
            return Error.Validation(Resources.OnboardingState_RequiresCompletedDate);
        }

        var progressPercentage = totalWeight > 0
            ? (int)((double)completedWeight / totalWeight * 100)
            : 0;

        return new CurrentStepState(
            status,
            currentStepId,
            pathTaken,
            pathAhead,
            totalWeight,
            completedWeight,
            progressPercentage,
            values,
            startedAt,
            completedAt,
            DateTime.UtcNow,
            completedBy);
    }

    private CurrentStepState(
        OnboardingStatus status,
        string currentStepId,
        Journey pathTaken,
        Journey pathAhead,
        int totalWeight,
        int completedWeight,
        int progressPercentage,
        StringNameValues values,
        Optional<DateTime> startedAt,
        Optional<DateTime> completedAt,
        DateTime enteredAt,
        Optional<string> completedBy)
    {
        Status = status;
        CurrentStepId = currentStepId;
        PathTaken = pathTaken;
        PathAhead = pathAhead;
        TotalWeight = totalWeight;
        CompletedWeight = completedWeight;
        ProgressPercentage = progressPercentage;
        AllValues = values;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        EnteredAt = enteredAt;
        CompletedBy = completedBy;
    }

    public StringNameValues AllValues { get; }

    public Optional<DateTime> CompletedAt { get; }

    public Optional<string> CompletedBy { get; }

    public int CompletedWeight { get; }

    public string CurrentStepId { get; }

    public DateTime EnteredAt { get; }

    public Journey PathAhead { get; }

    public Journey PathTaken { get; }

    public int ProgressPercentage { get; }

    public Optional<DateTime> StartedAt { get; }

    public OnboardingStatus Status { get; private set; }

    public int TotalWeight { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return
        [
            Status,
            CurrentStepId,
            PathTaken,
            PathAhead,
            TotalWeight,
            CompletedWeight,
            ProgressPercentage,
            AllValues,
            StartedAt,
            CompletedAt,
            EnteredAt,
            CompletedBy.ValueOrDefault
        ];
    }

    [UsedImplicitly]
    public static ValueObjectFactory<CurrentStepState> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new CurrentStepState(
                parts[0].Value.ToEnumOrDefault(OnboardingStatus.NotStarted),
                parts[1],
                Journey.Rehydrate()(parts[2], container),
                Journey.Rehydrate()(parts[3], container),
                parts[4].Value.ToIntOrDefault(0),
                parts[5].Value.ToIntOrDefault(0),
                parts[6].Value.ToIntOrDefault(0),
                StringNameValues.Rehydrate()(parts[7], container),
                parts[8].ToOptional(val => val.FromIso8601()),
                parts[9].ToOptional(val => val.FromIso8601()),
                parts[10].Value.FromIso8601(),
                parts[11]);
        };
    }

    public Result<CurrentStepState, Error> MarkComplete(string completedBy)
    {
        return Create(
            OnboardingStatus.Complete,
            CurrentStepId,
            PathTaken,
            PathAhead,
            TotalWeight,
            CompletedWeight,
            AllValues,
            StartedAt,
            DateTime.UtcNow.ToOptional(),
            completedBy.ToOptional());
    }

    /// <summary>
    ///     Navigates the workflow to the next step, or previous step.
    ///     Assumes one step at a time (in the workflow).
    ///     Note: Whenever we visit a step, we copy any initial values from the schema, overrite some properties, and carry
    ///     forward other properties from previous steps.
    ///     Note: We need to populate the current step, calculate the journey ahead, and navigate to the next/previous step in
    ///     the journey.
    /// </summary>
    public Result<CurrentStepState, Error> NavigateToStep(IOnboardingWorkflowService workflowService, string fromStepId,
        string toStepId, WorkflowSchema workflow)
    {
        if (Status == OnboardingStatus.Complete)
        {
            return Error.RuleViolation(Resources.CurrentStepState_NavigateToStep_AlreadyCompleted);
        }

        if (fromStepId.NotEqualsIgnoreCase(CurrentStepId))
        {
            return Error.RuleViolation(
                Resources.CurrentStepState_NavigateToStep_NotFromCurrentStep.Format(fromStepId, CurrentStepId));
        }

        if (!workflow.TryGetStep(CurrentStepId, out var currentStepSchema))
        {
            return Error.RuleViolation(
                Resources.CurrentStepState_NavigateToStep_UnknownCurrentStep.Format(CurrentStepId));
        }

        var direction = PathTaken.Steps.HasAny() && (PathTaken.Steps
            .LastOrDefault()?.StepId
            .EqualsIgnoreCase(toStepId) ?? false)
            ? NavigationDirection.Backward
            : NavigationDirection.Forward;

        if (direction == NavigationDirection.Forward
            && currentStepSchema.Type == OnboardingStepType.End)
        {
            return Error.RuleViolation(
                Resources.CurrentStepState_NavigateToStep_ForwardFromEnd.Format(CurrentStepId));
        }

        if (direction == NavigationDirection.Backward
            && currentStepSchema.Type == OnboardingStepType.Start)
        {
            return Error.RuleViolation(
                Resources.CurrentStepState_NavigateToStep_BackwardFromStart.Format(CurrentStepId));
        }

        var currentStep = currentStepSchema.ToStep(EnteredAt, DateTime.UtcNow, AllValues);
        if (currentStep.IsFailure)
        {
            return currentStep.Error;
        }

        Journey newJourney;
        switch (direction)
        {
            case NavigationDirection.Backward:
            {
                var removed = PathTaken.RemoveLastStep();
                if (removed.IsFailure)
                {
                    return removed.Error;
                }

                newJourney = removed.Value;
                break;
            }

            case NavigationDirection.Forward:
            {
                var appended = PathTaken.AppendNextStep(currentStep.Value);
                if (appended.IsFailure)
                {
                    return appended.Error;
                }

                newJourney = appended.Value;
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        if (!workflow.TryGetStep(toStepId, out var toStepSchema))
        {
            return Error.RuleViolation(Resources.CurrentStepState_NavigateToStep_UnknownNextStep.Format(toStepId));
        }

        var nextStep = toStepSchema.ToStep(DateTime.UtcNow, Optional<DateTime>.None);
        if (nextStep.IsFailure)
        {
            return nextStep.Error;
        }

        var status = Status == OnboardingStatus.NotStarted
            ? OnboardingStatus.InProgress
            : Status;
        var completedWeight = direction == NavigationDirection.Backward
            ? CompletedWeight - nextStep.Value.Weight
            : CompletedWeight + currentStep.Value.Weight;
        var startedAt = StartedAt.HasValue
            ? StartedAt
            : DateTime.UtcNow.ToOptional();
        var pathAhead = workflow.Journeys.GetBestPathAhead(workflowService, toStepId, workflow.EndStepId);
        var values = direction == NavigationDirection.Forward
            ? currentStep.Value.Values.Merge(nextStep.Value.Values, DictionaryExtensions.MergeStrategy.Insert)
            : currentStep.Value.Values;

        return Create(
            status,
            nextStep.Value.StepId,
            newJourney,
            pathAhead,
            TotalWeight,
            completedWeight,
            values,
            startedAt,
            CompletedAt,
            CompletedBy);
    }

#if TESTINGONLY

    [SkipImmutabilityCheck]
    public void TestingOnly_SetStatus(OnboardingStatus status)
    {
        Status = status;
    }
#endif

    public Result<CurrentStepState, Error> UpdateCurrentStepValues(StringNameValues values)
    {
        var newJourney = PathTaken;
        if (newJourney.HasAny())
        {
            var lastStep = newJourney.Last();
            if (lastStep.HasValue)
            {
                var updatedStepValues = lastStep.Value.Values.Merge(values, DictionaryExtensions.MergeStrategy.Upsert);
                var updatedStep = lastStep.Value.ChangeValues(updatedStepValues);
                if (updatedStep.IsFailure)
                {
                    return updatedStep.Error;
                }

                lastStep = updatedStep.Value;
                var updatedJourney = newJourney.ReplaceLastStep(lastStep);
                if (updatedJourney.IsFailure)
                {
                    return updatedJourney.Error;
                }

                newJourney = updatedJourney.Value;
            }
        }

        var updatedValues = AllValues.Merge(values, DictionaryExtensions.MergeStrategy.Upsert);
        return Create(
            Status,
            CurrentStepId,
            newJourney,
            PathAhead,
            TotalWeight,
            CompletedWeight,
            updatedValues,
            StartedAt,
            CompletedAt,
            CompletedBy);
    }

    private enum NavigationDirection
    {
        Forward = 0,
        Backward = 1
    }
}