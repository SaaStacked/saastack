using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Common.Clients;

/// <summary>
///     Defines an JSON:API error, from <see href="https://jsonapi.org/format/1.0/#error-objects">JSON:API</see>
/// </summary>
[UsedImplicitly]
public class JsonApiProblemDetails
{
    public const string Reference = "https://jsonapi.org/format/1.0/#error-objects";

    [JsonPropertyName("errors")] public List<Error> Errors { get; set; } = [];

    [JsonPropertyName("jsonapi")] public JsonApiHeader? JsonApi { get; set; }

    /// <summary>
    ///     Defines the JSON:API header
    /// </summary>
    public class JsonApiHeader
    {
        [JsonPropertyName("version")] public string? Version { get; set; }
    }

    /// <summary>
    ///     Defines each error
    /// </summary>
    public class Error
    {
        [JsonPropertyName("code")] public string? Code { get; set; }

        [JsonPropertyName("detail")] public string? Detail { get; set; }

        [JsonPropertyName("id")] public string? Id { get; set; }

        [JsonPropertyName("links")] public Dictionary<string, object>? Links { get; set; }

        [JsonPropertyName("meta")] public Dictionary<string, object>? Meta { get; set; }

        [JsonPropertyName("source")] public ErrorSource? Source { get; set; }

        [JsonPropertyName("status")] public string? Status { get; set; }

        [JsonPropertyName("title")] public string? Title { get; set; }
    }

    /// <summary>
    ///     Defines the source of the error
    /// </summary>
    public class ErrorSource
    {
        [JsonPropertyName("parameter")] public string? Parameter { get; set; }

        [JsonPropertyName("pointer")] public string? Pointer { get; set; }
    }
}