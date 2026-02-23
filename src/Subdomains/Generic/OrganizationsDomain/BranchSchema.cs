using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace OrganizationsDomain;

public sealed class BranchSchema : ValueObjectBase<BranchSchema>
{
    public static Result<BranchSchema, Error> Create(string id, string label, BranchConditionSchema condition,
        string nextStepId)
    {
        if (id.IsInvalidParameter(i => i.HasValue(), nameof(id), Resources.BranchSchema_InvalidId, out var error1))
        {
            return error1;
        }

        if (label.IsInvalidParameter(lbl => lbl.HasValue(), nameof(label), Resources.BranchSchema_InvalidLabel,
                out var error2))
        {
            return error2;
        }

        if (nextStepId.IsInvalidParameter(Validations.Onboarding.Workflow.StepId, nameof(nextStepId),
                Resources.BranchSchema_InvalidNextStepId, out var error3))
        {
            return error3;
        }

        return new BranchSchema(id, label, condition, nextStepId);
    }

    private BranchSchema(string id, string label, BranchConditionSchema condition, string nextStepId)
    {
        Id = id;
        Label = label;
        Condition = condition;
        NextStepId = nextStepId;
    }

    public BranchConditionSchema Condition { get; }

    public string Id { get; }

    public string Label { get; }

    public string NextStepId { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Id, Label, Condition, NextStepId];
    }

    public static ValueObjectFactory<BranchSchema> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new BranchSchema(parts[0],
                parts[1],
                BranchConditionSchema.Rehydrate()(parts[2], container),
                parts[3]);
        };
    }
}