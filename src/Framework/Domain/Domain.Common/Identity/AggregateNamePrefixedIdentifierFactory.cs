using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Validations;

namespace Domain.Common.Identity;

/// <summary>
///     Provides a <see cref="IIdentifierFactory" /> that creates identifiers for aggregates that are prefixed using the
///     name of the aggregate/entity,  followed by a simplified and base64 encoded version of a guid.
///     For example: a UserAccount aggregate might have the prefix "user_"
/// </summary>
public abstract class AggregateNamePrefixedIdentifierFactory : IIdentifierFactory
{
    private const string Delimiter = "_";
    private const string UnknownAggregatePrefix = "xxx";
    private readonly IDictionary<Type, string> _aggregatePrefixes;
    private readonly List<string> _supportedPrefixes = new();

    protected AggregateNamePrefixedIdentifierFactory(IDictionary<Type, string> aggregatePrefixes)
    {
        aggregatePrefixes.Merge(new Dictionary<Type, string>(aggregatePrefixes)
            { { typeof(EventSourcedChangeEvent), "event" } });
        _aggregatePrefixes = aggregatePrefixes;
    }

#if TESTINGONLY
    // ReSharper disable once CollectionNeverQueried.Global
    public Dictionary<string, string> LastCreatedIds { get; } = new();
#endif

    public IEnumerable<Type> RegisteredTypes => _aggregatePrefixes.Keys;

    public IReadOnlyList<string> SupportedPrefixes => _supportedPrefixes;

    /// <summary>
    ///     Creates an identifier for the specified registered aggregate
    /// </summary>
    public Result<Identifier, Error> Create(IIdentifiableEntity entity)
    {
        var entityType = entity.GetType();
        var prefix = _aggregatePrefixes.ContainsKey(entityType)
            ? _aggregatePrefixes[entity.GetType()]
            : UnknownAggregatePrefix;

        var guid = Guid.NewGuid();
        var identifier = ConvertGuid(guid, prefix);
        return identifier.Match<Result<Identifier, Error>>(id =>
        {
#if TESTINGONLY
            LastCreatedIds.Add(id.Value, guid.ToString("D"));
#endif
            return id.Value.ToId();
        }, error => error);
    }

    /// <summary>
    ///     Whether the identifier is of the form expected, and has a known prefix
    /// </summary>
    public bool IsValid(Identifier value)
    {
        var id = value.ToString();
        var delimiterIndex = id.IndexOf(Delimiter, StringComparison.Ordinal);
        if (delimiterIndex == -1)
        {
            return false;
        }

        var prefix = id.Substring(0, delimiterIndex);
        if (!IsKnownPrefix(prefix) && prefix != UnknownAggregatePrefix)
        {
            return false;
        }

        return CommonValidations.Identifier.Matches(id);
    }

    public Result<Error> AddSupportedPrefix(string prefix)
    {
        if (prefix.IsInvalidParameter(CommonValidations.IdentifierPrefix, nameof(prefix), null,
                out var error))
        {
            return error;
        }

        _supportedPrefixes.Add(prefix);

        return Result.Ok;
    }

    /// <summary>
    ///     Converts a guid and prefix to an identifier
    /// </summary>
    internal static Result<string, Error> ConvertGuid(Guid guid, string prefix)
    {
        if (prefix.IsNotValuedParameter(nameof(prefix), out var error))
        {
            return error;
        }

        var random = Convert.ToBase64String(guid.ToByteArray())
            .Replace("+", string.Empty)
            .Replace("/", string.Empty)
            .Replace("=", string.Empty);

        return $"{prefix}{Delimiter}{random}";
    }

    private bool IsKnownPrefix(string prefix)
    {
        var allPossiblePrefixes = _aggregatePrefixes.Select(pre => pre.Value)
            .Concat(SupportedPrefixes)
            .Distinct();

        return allPossiblePrefixes.Contains(prefix);
    }
}