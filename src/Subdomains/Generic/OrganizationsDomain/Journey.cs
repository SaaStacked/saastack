using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace OrganizationsDomain;

public sealed class Journey : SingleValueObjectBase<Journey, List<Step>>
{
    public static readonly Journey Empty = new([]);

    public static Result<Journey, Error> Create(IReadOnlyList<Step> steps)
    {
        return new Journey(steps.ToList());
    }

    private Journey(List<Step> steps) : base(steps)
    {
    }

    public IReadOnlyList<Step> Steps => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<Journey> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            var steps = items.Select(item => Step.Rehydrate()(item, container));

            return new Journey(steps.ToList());
        };
    }

    public Result<Journey, Error> AppendNextStep(Step step)
    {
        return Create(Value.Append(step).ToList());
    }

    [SkipImmutabilityCheck]
    public bool HasAny()
    {
        return Value.HasAny();
    }

    [SkipImmutabilityCheck]
    public bool IsEmpty()
    {
        return Value.HasNone();
    }

    [SkipImmutabilityCheck]
    public Optional<Step> Last()
    {
        return Value.LastOrDefault();
    }

    public Result<Journey, Error> RemoveLastStep()
    {
        var steps = Value.ToList();
        if (steps.HasNone())
        {
            return Error.RuleViolation(Resources.Journey_RemoveLast_NoSteps);
        }

        steps.RemoveAt(steps.Count - 1);
        return Create(steps);
    }

    public Result<Journey, Error> ReplaceLastStep(Step newStep)
    {
        var steps = Value.ToList();
        if (steps.HasNone())
        {
            return Error.RuleViolation(Resources.Journey_ReplaceLastStep_NoSteps);
        }

        steps[^1] = newStep;
        return Create(steps);
    }

    public Result<Journey, Error> TruncateFirstStep()
    {
        if (IsEmpty())
        {
            return this;
        }

        var steps = Value.ToList();
        steps.RemoveAt(0);
        return Create(steps);
    }
}