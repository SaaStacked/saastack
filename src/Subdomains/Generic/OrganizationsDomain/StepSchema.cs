using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using JetBrains.Annotations;

namespace OrganizationsDomain;

public sealed class StepSchema : ValueObjectBase<StepSchema>
{
    public static Result<StepSchema, Error> Create(
        string id,
        OnboardingStepType type,
        string title,
        Optional<string> description,
        Optional<string> nextStepId,
        IReadOnlyList<BranchSchema> branches,
        int weight,
        IReadOnlyDictionary<string, string> values)
    {
        if (id.IsInvalidParameter(i => i.HasValue(), nameof(id), Resources.StepSchema_InvalidId, out var error1))
        {
            return error1;
        }

        if (title.IsInvalidParameter(tlt => tlt.HasValue(), nameof(title), Resources.StepSchema_InvalidTitle,
                out var error2))
        {
            return error2;
        }

        if (weight.IsInvalidParameter(wt => wt is >= 0 and <= 100, nameof(weight), Resources.StepSchema_InvalidWeight,
                out var error3))
        {
            return error3;
        }

        if (nextStepId.HasValue)
        {
            if (nextStepId.Value.IsInvalidParameter(Validations.Onboarding.Workflow.StepId, nameof(nextStepId),
                    Resources.StepSchema_InvalidNextStepId, out var error4))
            {
                return error4;
            }
        }

        if (type == OnboardingStepType.Branch && branches.Count == 0)
        {
            return Error.Validation(Resources.StepSchema_BranchStepWithoutBranchDefinition);
        }

        if (type != OnboardingStepType.Branch && branches.Count > 0)
        {
            return Error.Validation(Resources.StepSchema_NonBranchStepWithBranchDefinition);
        }

        if (type == OnboardingStepType.Branch && nextStepId.HasValue)
        {
            return Error.Validation(Resources.StepSchema_BranchStepCannotHaveNextStepId);
        }

        if (type is OnboardingStepType.Start or OnboardingStepType.Normal && !nextStepId.HasValue)
        {
            return Error.Validation(string.Format(Resources.StepSchema_NonTerminatingNodeWithoutNextStep, type));
        }

        var onboardingBranches = BranchesSchema.Create(branches);
        if (onboardingBranches.IsFailure)
        {
            return onboardingBranches.Error;
        }

        var stringValues = StringNameValues.Create(values);
        if (stringValues.IsFailure)
        {
            return stringValues.Error;
        }

        return new StepSchema(id, type, title, description, nextStepId, onboardingBranches.Value, weight,
            stringValues.Value);
    }

    private StepSchema(
        string id,
        OnboardingStepType type,
        string title,
        Optional<string> description,
        Optional<string> nextStepId,
        BranchesSchema branches,
        int weight,
        StringNameValues initialValues)
    {
        Id = id;
        Type = type;
        Title = title;
        Description = description;
        NextStepId = nextStepId;
        Branches = branches;
        Weight = weight;
        InitialValues = initialValues;
    }

    public BranchesSchema Branches { get; }

    public Optional<string> Description { get; }

    public string Id { get; }

    public StringNameValues InitialValues { get; }

    public Optional<string> NextStepId { get; }

    public string Title { get; }

    public OnboardingStepType Type { get; }

    public int Weight { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return
        [
            Id, Type, Title, Description, NextStepId,
            Branches, Weight, InitialValues
        ];
    }

    [UsedImplicitly]
    public static ValueObjectFactory<StepSchema> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new StepSchema(parts[0],
                parts[1].Value.ToEnumOrDefault(OnboardingStepType.Start),
                parts[2],
                parts[3],
                parts[4],
                BranchesSchema.Rehydrate()(parts[5], container),
                parts[6].Value.ToIntOrDefault(0),
                StringNameValues.Rehydrate()(parts[7], container));
        };
    }

    [SkipImmutabilityCheck]
    public Result<Step, Error> ToStep()
    {
        return ToStep(Optional<DateTime>.None, Optional<DateTime>.None, InitialValues);
    }

    [SkipImmutabilityCheck]
    public Result<Step, Error> ToStep(Optional<DateTime> enteredAt, Optional<DateTime> lastUpdatedAt)
    {
        return ToStep(enteredAt, lastUpdatedAt, InitialValues);
    }

    [SkipImmutabilityCheck]
    public Result<Step, Error> ToStep(Optional<DateTime> enteredAt, Optional<DateTime> lastUpdatedAt,
        StringNameValues values)
    {
        return Step.Create(Id, Title, Weight, enteredAt, lastUpdatedAt, values);
    }
}

public enum OnboardingStepType
{
    Start = 0,
    Normal = 1,
    Branch = 2,
    End = 3
}