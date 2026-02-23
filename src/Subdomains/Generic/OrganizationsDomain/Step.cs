using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared;
using JetBrains.Annotations;

namespace OrganizationsDomain;

public sealed class Step : ValueObjectBase<Step>
{
    public static Result<Step, Error> Create(
        string stepId,
        string title,
        int weight,
        Optional<DateTime> enteredAt,
        Optional<DateTime> lastUpdatedAt,
        StringNameValues values)
    {
        if (stepId.IsInvalidParameter(Validations.Onboarding.Workflow.StepId, nameof(stepId),
                Resources.Step_InvalidStepId, out var error1))
        {
            return error1;
        }

        if (title.IsInvalidParameter(Validations.Onboarding.Workflow.StepTitle, nameof(title),
                Resources.Step_InvalidTitle, out var error2))
        {
            return error2;
        }

        if (weight.IsInvalidParameter(wt => wt is >= 0 and <= 100, nameof(weight), Resources.Step_InvalidWeight,
                out var error3))
        {
            return error3;
        }

        return new Step(stepId, title, weight, enteredAt, lastUpdatedAt, values);
    }

    private Step(
        string stepId,
        string title,
        int weight,
        Optional<DateTime> enteredAt,
        Optional<DateTime> lastUpdatedAt,
        StringNameValues values)
    {
        StepId = stepId;
        Title = title;
        Weight = weight;
        EnteredAt = enteredAt;
        LastUpdatedAt = lastUpdatedAt;
        Values = values;
    }

    public Optional<DateTime> EnteredAt { get; }

    public Optional<DateTime> LastUpdatedAt { get; }

    public string StepId { get; }

    public string Title { get; }

    public StringNameValues Values { get; }

    public int Weight { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [StepId, Title, Weight, EnteredAt, LastUpdatedAt, Values];
    }

    [UsedImplicitly]
    public static ValueObjectFactory<Step> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new Step(parts[0], parts[1], parts[2].Value.ToIntOrDefault(0),
                parts[3].ToOptional(val => val.FromIso8601()),
                parts[4].ToOptional(val => val.FromIso8601()),
                StringNameValues.Rehydrate()(parts[65], container));
        };
    }

    public Result<Step, Error> ChangeValues(StringNameValues values)
    {
        return Create(StepId, Title, Weight, EnteredAt, LastUpdatedAt, values);
    }
}