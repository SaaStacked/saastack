using Common.Extensions;
using Domain.Shared;

namespace OrganizationsDomain.Extensions;

public static class DictionaryExtensions
{
    public enum MergeStrategy
    {
        /// <summary>
        ///     Update values if they already exist, insert them if they do not exist
        /// </summary>
        Upsert,

        /// <summary>
        ///     Insert values if they do not exist, skip them if they do exist
        /// </summary>
        Insert
    }

    public static StringNameValues Merge(this StringNameValues source, StringNameValues other,
        MergeStrategy strategy)
    {
        return StringNameValues.Create(source.Items.Merge(other.Items, strategy)).Value;
    }

    public static Dictionary<string, string> Merge(this IReadOnlyDictionary<string, string> source,
        IReadOnlyDictionary<string, string> other, MergeStrategy strategy)
    {
        if (source.HasNone())
        {
            return other.ToDictionary();
        }

        if (other.HasNone())
        {
            return source.ToDictionary();
        }

        var merged = new Dictionary<string, string>(source);
        foreach (var kvp in other)
        {
            switch (strategy)
            {
                case MergeStrategy.Upsert:
                    merged[kvp.Key] = kvp.Value;
                    break;

                case MergeStrategy.Insert:
                    merged.TryAdd(kvp.Key, kvp.Value);
                    break;
            }
        }

        return merged;
    }
}