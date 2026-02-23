using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared;

public sealed class StringNameValues : SingleValueObjectBase<StringNameValues, IReadOnlyDictionary<string, string>>
{
    public static readonly StringNameValues Empty = new(new Dictionary<string, string>());

    public static Result<StringNameValues, Error> Create(IReadOnlyDictionary<string, string> values)
    {
        if (values.HasAny())
        {
            foreach (var value in values)
            {
                if (value.Value.IsNotValuedParameter(nameof(values),
                        Resources.StringNameValues_InvalidValue.Format(value.Key), out var error))
                {
                    return error;
                }
            }
        }

        return new StringNameValues(values);
    }

    private StringNameValues(IReadOnlyDictionary<string, string> values) : base(values)
    {
    }

    public IReadOnlyDictionary<string, string> Items => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<StringNameValues> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new StringNameValues(parts[0].Value.FromJson<Dictionary<string, string>>()!);
        };
    }
}