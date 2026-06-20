using System.Collections;
using Common.Extensions;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.ValueObjects;

/// <inheritdoc cref="ValueObjectBase{TValueObject}" />
public abstract partial class ValueObjectBase<TValueObject> : IEqualityComparer,
    IEqualityComparer<ValueObjectBase<TValueObject>>
{
    [SkipImmutabilityCheck]
    bool IEqualityComparer.Equals(object? left, object? right)
    {
        if (left.NotExists())
        {
            return right.NotExists();
        }

        if (right.NotExists())
        {
            return false;
        }

        if (left.GetType() != right.GetType())
        {
            return false;
        }

        if (left.GetType() != GetType()
            || right.GetType() != GetType())
        {
            return false;
        }

        return ((ValueObjectBase<TValueObject>)left).Equals(right);
    }

    [SkipImmutabilityCheck]
    public int GetHashCode(object instance)
    {
        if (instance.GetType() != GetType())
        {
            return instance.GetHashCode();
        }

        return ((ValueObjectBase<TValueObject>)instance).GetHashCode();
    }

    [SkipImmutabilityCheck]
    public bool Equals(ValueObjectBase<TValueObject>? left, ValueObjectBase<TValueObject>? right)
    {
        if (left.NotExists())
        {
            return false;
        }

        if (right.NotExists())
        {
            return false;
        }

        return left.Equals(right);
    }

    [SkipImmutabilityCheck]
    public int GetHashCode(ValueObjectBase<TValueObject> instance)
    {
        return instance.GetHashCode();
    }

    [SkipImmutabilityCheck]
    public override bool Equals(object? other)
    {
        if (other.NotExists()
            || other.GetType() != GetType())
        {
            return false;
        }

        var valueInstance = (ValueObjectBase<TValueObject>)other;

        using var leftValues = GetAtomicValues().GetEnumerator();
        using var rightValues = valueInstance.GetAtomicValues().GetEnumerator();

        while (leftValues.MoveNext() && rightValues.MoveNext())
        {
            if (ReferenceEquals(leftValues.Current, null) != ReferenceEquals(rightValues.Current, null))
            {
                return false;
            }

            var leftValue = leftValues.Current;
            var rightValue = rightValues.Current;

            if (leftValue.NotExists())
            {
                continue;
            }

            if (leftValue is IDictionary leftDictionary
                && rightValue is IDictionary rightDictionary)
            {
                if (!DictionariesAreEqual(leftDictionary, rightDictionary))
                {
                    return false;
                }
            }
            else if (leftValue is IEnumerable<IDehydratableValueObject> leftEnumerable
                     && rightValue is IEnumerable<IDehydratableValueObject> rightEnumerable)
            {
                if (!leftEnumerable.SequenceEqual(rightEnumerable))
                {
                    return false;
                }
            }
            else if (!leftValue.Equals(rightValue))
            {
                return false;
            }
        }

        return leftValues.MoveNext() == rightValues.MoveNext();
    }

    [SkipImmutabilityCheck]
    public bool Equals(string other)
    {
        if (other.NotExists())
        {
            return false;
        }

        var value = Dehydrate();
        if (value.HasNoValue())
        {
            return false;
        }

        return value.EqualsOrdinal(other);
    }

    [SkipImmutabilityCheck]
    public override int GetHashCode()
    {
        return GetAtomicValues()
            .Select(val =>
            {
                if (val.NotExists())
                {
                    return 0;
                }

                if (val is string stringValue)
                {
                    return GetDeterministicHashCode(stringValue);
                }

                if (val is IDictionary dict)
                {
                    return GetDictionaryHashCode(dict);
                }

                return val.GetHashCode();
            })
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObjectBase<TValueObject> left, ValueObjectBase<TValueObject> right)
    {
        if (left.NotExists())
        {
            return right.NotExists();
        }

        return left.Equals(right);
    }

    public static bool operator ==(ValueObjectBase<TValueObject> left, string right)
    {
        if (left.NotExists())
        {
            return right.NotExists();
        }

        return left.Equals(right);
    }

    public static bool operator !=(ValueObjectBase<TValueObject> left, ValueObjectBase<TValueObject> right)
    {
        if (left.NotExists())
        {
            return true;
        }

        return !(left == right);
    }

    public static bool operator !=(ValueObjectBase<TValueObject> left, string right)
    {
        if (left.NotExists())
        {
            return true;
        }

        return !(left == right);
    }

    private static bool DictionariesAreEqual(IDictionary left, IDictionary right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (DictionaryEntry entry in left)
        {
            if (!right.Contains(entry.Key))
            {
                return false;
            }

            var leftValue = entry.Value;
            var rightValue = right[entry.Key];

            if (ReferenceEquals(leftValue, null) != ReferenceEquals(rightValue, null))
            {
                return false;
            }

            if (leftValue is null)
            {
                continue;
            }

            if (!leftValue.Equals(rightValue))
            {
                return false;
            }
        }

        return true;
    }

    private static int GetDictionaryHashCode(IDictionary dict)
    {
        return dict.Keys.Cast<object>()
            .OrderBy(key => key.ToString())
            .Aggregate(0, (hash, key) =>
            {
                var keyHash = key is string s
                    ? GetDeterministicHashCode(s)
                    : key.GetHashCode();
                var value = dict[key];
                var valueHash = value is null ? 0
                    : value is string sv ? GetDeterministicHashCode(sv)
                    : value.GetHashCode();
                return hash ^ keyHash ^ valueHash;
            });
    }

    /// <summary>
    ///     We want a deterministic hashing function for a given string to be the same everytime.
    ///     Since the <see cref="string.GetHashCode()" /> function now  returns randomized hashes for security purposes (since
    ///     .net4.5).
    ///     We are not using this hash outside of this codebase, and not reading in hashes from outside, so we are avoiding the
    ///     vulnerability. See
    ///     <see href="https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/">GetHashCode</see>
    ///     for more details
    /// </summary>
    protected static int GetDeterministicHashCode(string str)
    {
        unchecked
        {
            var hash1 = (5381 << 16) + 5381;
            var hash2 = hash1;

            for (var i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1)
                {
                    break;
                }

                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + hash2 * 1566083941;
        }
    }
}