using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;

namespace OrganizationsDomain;

public sealed class BranchConditionSchema : ValueObjectBase<BranchConditionSchema>
{
    public static Result<BranchConditionSchema, Error> Create(
        BranchConditionOperator op,
        string field,
        string value)
    {
        if (field.IsInvalidParameter(fld => fld.HasValue(), nameof(field), Resources.BranchConditionschema_InvalidName,
                out var error1))
        {
            return error1;
        }

        if (value.IsInvalidParameter(val => val.Exists(), nameof(value), Resources.BranchConditionschema_InvalidValue,
                out var error2))
        {
            return error2;
        }

        return new BranchConditionSchema(op, field, value);
    }

    private BranchConditionSchema(BranchConditionOperator op, string field, string value)
    {
        Operator = op;
        Field = field;
        Value = value;
    }

    public string Field { get; }

    public BranchConditionOperator Operator { get; }

    public string Value { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Operator, Field, Value];
    }

    public static ValueObjectFactory<BranchConditionSchema> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new BranchConditionSchema(parts[0].Value.ToEnumOrDefault(BranchConditionOperator.Equals),
                parts[1],
                parts[2]);
        };
    }

    [SkipImmutabilityCheck]
    public bool Evaluate(IReadOnlyDictionary<string, string> values)
    {
        if (!values.TryGetValue(Field, out var actualValue))
        {
            return false;
        }

        return Operator switch
        {
            BranchConditionOperator.Equals => actualValue == Value,
            BranchConditionOperator.Contains => actualValue.Contains(Value, StringComparison.OrdinalIgnoreCase),
            BranchConditionOperator.GreaterThan => double.TryParse(actualValue, out var actual) &&
                                                   double.TryParse(Value, out var expected) &&
                                                   actual > expected,
            BranchConditionOperator.LessThan => double.TryParse(actualValue, out var actual2) &&
                                                double.TryParse(Value, out var expected2) &&
                                                actual2 < expected2,
            _ => false
        };
    }
}

public enum BranchConditionOperator
{
    Equals = 0,
    Contains = 1,
    GreaterThan = 2,
    LessThan = 3
}