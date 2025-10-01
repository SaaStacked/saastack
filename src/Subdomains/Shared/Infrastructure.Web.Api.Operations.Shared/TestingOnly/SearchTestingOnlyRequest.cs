#if TESTINGONLY
using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

/// <summary>
///     Tests the use of reference types, values types, and enums anywhere in a SEARCH request
/// </summary>
[Route("/testingonly/general/search/a/{AnEnumRouteProperty}/b/{AnIntRouteProperty}/c/{AStringRouteProperty}",
    OperationMethod.Search, isTestingOnly: true)]
public class SearchTestingOnlyRequest : UnTenantedSearchRequest<SearchTestingOnlyRequest, SearchTestingOnlyResponse>
{
    [JsonPropertyName("a_camel_enum")] public TestingOnlyEnum? ACamelEnumProperty { get; set; }

    [JsonPropertyName("a_camel_int")] public int? ACamelIntProperty { get; set; }

    [JsonPropertyName("a_camel_string")] public string? ACamelStringProperty { get; set; }

    public TestingOnlyEnum? AnEnumProperty { get; set; }

    public TestingOnlyEnum? AnEnumQueryProperty { get; set; }

    public TestingOnlyEnum? AnEnumRouteProperty { get; set; }

    public int? AnIntProperty { get; set; }

    public int? AnIntQueryProperty { get; set; }

    public int? AnIntRouteProperty { get; set; }

    public string? AStringProperty { get; set; }

    public string? AStringQueryProperty { get; set; }

    public string? AStringRouteProperty { get; set; }
}
#endif