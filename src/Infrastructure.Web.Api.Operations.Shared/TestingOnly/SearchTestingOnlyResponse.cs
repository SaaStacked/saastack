#if TESTINGONLY
using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.TestingOnly;

public class SearchTestingOnlyResponse : SearchResponse
{
    public List<TestResource> Items { get; set; } = [];

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