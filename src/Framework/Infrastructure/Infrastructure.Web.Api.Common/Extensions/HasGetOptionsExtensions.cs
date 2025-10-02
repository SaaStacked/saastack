﻿using Application.Interfaces;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class HasGetOptionsExtensions
{
    /// <summary>
    ///     Converts a <see cref="GetOptions" /> to its representation on the wire
    /// </summary>
    public static string? ToEmbed(this GetOptions options)
    {
        var hasOptions = options.ToHasGetOptions();
        return hasOptions.Embed;
    }

    /// <summary>
    ///     Converts a <see cref="IHasGetOptions" /> to a <see cref="GetOptions" />
    /// </summary>
    public static GetOptions ToGetOptions(this IHasGetOptions requestDto, ExpandOptions? defaultExpand = null,
        List<string>? defaultChildResources = null)
    {
        if (requestDto.NotExists())
        {
            return new GetOptions();
        }

        var embedValue = requestDto.Embed;
        if (embedValue.HasNoValue())
        {
            if (defaultChildResources.Exists()
                && defaultChildResources.HasAny())
            {
                return GetOptions.Custom(defaultChildResources);
            }

            if (defaultExpand.NotExists())
            {
                return requestDto is IHasSearchOptions
                    ? new GetOptions(ExpandOptions.None)
                    : new GetOptions(ExpandOptions.All);
            }

            return new GetOptions(defaultExpand.Value);
        }

        if (embedValue.EqualsIgnoreCase(HasGetOptions.EmbedNone))
        {
            return GetOptions.None;
        }

        if (embedValue.EqualsIgnoreCase(HasGetOptions.EmbedAll))
        {
            return GetOptions.All;
        }

        var values = (requestDto.Embed ?? string.Empty).Split(GetOptions.EmbedRequestParamDelimiter)
            .Select(value => value.ToLowerInvariant()
                .Trim())
            .ToList();

        return GetOptions.Custom(values);
    }

    /// <summary>
    ///     Converts a <see cref="GetOptions" /> to a <see cref="HasGetOptions" />
    /// </summary>
    public static HasGetOptions ToHasGetOptions(this GetOptions options)
    {
        if (options.NotExists())
        {
            return new HasGetOptions();
        }

        var childResourcesList = options.ResourceReferences.HasAny()
            ? options.ResourceReferences.Join(GetOptions.EmbedRequestParamDelimiter)
            : null;

        return new HasGetOptions
        {
            Embed = options.Expand == ExpandOptions.Custom
                ? childResourcesList
                : null
        };
    }
}