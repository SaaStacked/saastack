using System.Reflection;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.External.Persistence.Common.Extensions;

public static class EventStoreExtensions
{
    private static readonly IReadOnlyList<string> AggregateTypeSuffixesToTrim = ["Entity", "Aggregate", "Root"];

    /// <summary>
    ///     Returns the simplified name of the aggregate root type
    ///     Note: This extends <see cref="QueryExtensions.GetEntityNameSafe" /> to deal with naming conventions of this
    ///     codebase.
    /// </summary>
    public static string GetAggregateName<TAggregateRoot>()
    {
        var customAttribute = typeof(TAggregateRoot).GetCustomAttribute<EntityNameAttribute>();
        if (customAttribute.Exists())
        {
            return customAttribute.EntityName;
        }

        var typeName = typeof(TAggregateRoot).Name;
        foreach (var suffix in AggregateTypeSuffixesToTrim)
        {
            if (!typeName.EndsWith(suffix))
            {
                continue;
            }

            typeName = typeName.Substring(0, typeName.LastIndexOf(suffix, StringComparison.Ordinal));
            break;
        }

        return typeName;
    }

    /// <summary>
    ///     Verifies that the version of the latest event produced by the aggregate is the next event in the stream of events
    ///     from the store, with no version gaps between them. IN other words, they are contiguous
    /// </summary>
    public static Result<Error> VerifyContiguousCheck(this IEventStore eventStore, string streamName,
        Optional<int> latestStoredEventVersion, int nextEventVersion)
    {
        if (!latestStoredEventVersion.HasValue)
        {
            if (nextEventVersion != EventStream.FirstVersion)
            {
                var storeType = eventStore.GetType().Name;
                return Error.EntityExists(
                    Resources.EventStore_ConcurrencyVerificationFailed_StreamReset.Format(storeType, streamName));
            }

            return Result.Ok;
        }

        var expectedNextVersion = latestStoredEventVersion + 1;
        if (nextEventVersion > expectedNextVersion)
        {
            var storeType = eventStore.GetType().Name;
            return Error.EntityExists(
                Resources.EventStore_ConcurrencyVerificationFailed_MissingUpdates.Format(storeType, streamName,
                    expectedNextVersion, nextEventVersion));
        }

        return Result.Ok;
    }
}