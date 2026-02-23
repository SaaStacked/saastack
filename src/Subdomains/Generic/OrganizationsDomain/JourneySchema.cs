using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;
using OrganizationsDomain.DomainServices;

namespace OrganizationsDomain;

public sealed class JourneySchema : SingleValueObjectBase<JourneySchema, Dictionary<string, StepSchema>>
{
    public static readonly JourneySchema Empty = new([]);

    public static Result<JourneySchema, Error> Create(IReadOnlyDictionary<string, StepSchema> steps)
    {
        return new JourneySchema(steps.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    private JourneySchema(Dictionary<string, StepSchema> steps) : base(steps)
    {
    }

    public IReadOnlyDictionary<string, StepSchema> Steps => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<JourneySchema> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToStringDictionary(property);
            var steps = items.ToDictionary(item => item.Key, item => StepSchema.Rehydrate()(item.Value, container));

            return new JourneySchema(steps);
        };
    }

    [SkipImmutabilityCheck]
    public StepSchema Get(string stepId)
    {
        return Value[stepId];
    }

    /// <summary>
    ///     Calculates the best path ahead from the current step to the end step, as we proceed forward,
    ///     not including the current step.
    /// </summary>
    [SkipImmutabilityCheck]
    public Journey GetBestPathAhead(IOnboardingWorkflowService workflowService, string fromStepId,
        string endStepId)
    {
        var pathAhead = workflowService
            .CalculateShortestPathToEnd(Value, fromStepId, endStepId);
        if (pathAhead.IsEmpty())
        {
            return Journey.Empty;
        }

        if (pathAhead.Steps[0].StepId.EqualsIgnoreCase(fromStepId))
        {
            pathAhead = pathAhead.TruncateFirstStep().Value;
        }

        return pathAhead;
    }

    [SkipImmutabilityCheck]
    public bool TryGetValue(string stepId, [NotNullWhen(true)] out StepSchema? step)
    {
        return Value.TryGetValue(stepId, out step);
    }
}