using System.Text.Json.Serialization;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using RouteAttribute = Infrastructure.Web.Api.Interfaces.RouteAttribute;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Infrastructure.Web.Api.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class RequestExtensionsSpec
{
    private const string EmptyRequestSignature =
        "sha256=f8dbae1fc1114a368a46f762db4a5ad5417e0e1ea4bc34d7924d166621c45653";

    [Fact]
    public void WhenSetHMACAuthWithHttpRequestMessage_ThenSetsSignatureHeader()
    {
        var message = new HttpRequestMessage();

        message.SetHMACAuth("asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetHMACAuthWithEmptyPostRequest_ThenSetsSignatureHeader()
    {
        var request = new TestEmptyPostRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetHMACAuthWithEmptyPutPatchRequest_ThenSetsSignatureHeader()
    {
        var request = new TestEmptyPutPatchRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetHMACAuthWithEmptyGetRequest_ThenSetsSignatureHeader()
    {
        var request = new TestEmptyGetRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetHMACAuthWithEmptyDeleteRequest_ThenSetsSignatureHeader()
    {
        var request = new TestEmptyDeleteRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetHMACAuthWithPopulatedPostRequest_ThenSetsSignatureHeader()
    {
        var request = new TestPopulatedPostRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should().OnlyContain(hdr =>
            hdr == "sha256=39c08f3c039a00cbe36df51b325b058daef9ce54bc9a1b21eba71c048ca68c6c");
    }

    [Fact]
    public void WhenSetHMACAuthWithPopulatedPutPatchRequest_ThenSetsSignatureHeader()
    {
        var request = new TestPopulatedPutPatchRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should().OnlyContain(hdr =>
            hdr == "sha256=39c08f3c039a00cbe36df51b325b058daef9ce54bc9a1b21eba71c048ca68c6c");
    }

    [Fact]
    public void WhenSetHMACAuthWithPopulatedGetRequest_ThenSetsSignatureHeader()
    {
        var request = new TestPopulatedGetRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetHMACAuthWithPopulatedDeleteRequest_ThenSetsSignatureHeader()
    {
        var request = new TestPopulatedDeleteRequest();
        var message = new HttpRequestMessage();

        message.SetHMACAuth(request, "asecret");

        message.Headers.GetValues(HttpConstants.Headers.HMACSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenGetRequestInfoAndNoAttribute_ThenThrows()
    {
        var request = new NoRouteRequest();

        request.Invoking(x => x.GetRequestInfo())
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                Resources.RequestExtensions_MissingRouteAttribute.Format(nameof(NoRouteRequest),
                    nameof(RouteAttribute)));
    }

    [Fact]
    public void WhenGetRequestInfoAndRequestHasNoFields_ThenReturnsInfo()
    {
        var request = new HasNoPropertiesRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/{unknown}");
        result.Method.Should().Be(OperationMethod.Get);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateContainsNoPlaceholdersWithNoDataForGet_ThenReturnsInfo()
    {
        var request = new HasNoPlaceholdersGetRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute");
        result.Method.Should().Be(OperationMethod.Get);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateContainsNoPlaceholdersWithDataForGet_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasNoPlaceholdersGetRequest
        {
            Id = "anid",
            ANumberProperty = 999,
            ADateTimeProperty = datum,
            AStringProperty = "avalue"
        };

        var result = request.GetRequestInfo();

        result.Route.Should()
            .Be(
                "/aroute?adatetimeproperty=2023-10-29T12%3a30%3a15Z&anumberproperty=999&astringproperty=avalue&id=anid");
        result.Method.Should().Be(OperationMethod.Get);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateContainsNoPlaceholdersWithDataForPost_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasNoPlaceholdersPostRequest
        {
            Id = "anid",
            ANumberProperty = 999,
            ADateTimeProperty = datum,
            AStringProperty = "avalue"
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute");
        result.Method.Should().Be(OperationMethod.Post);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasUnknownPlaceholderWithNoDataForGet_ThenReturnsInfo()
    {
        var request = new HasUnknownPlaceholderGetRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/{unknown}");
        result.Method.Should().Be(OperationMethod.Get);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasUnknownPlaceholderWithDataForGet_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasUnknownPlaceholderGetRequest
        {
            Id = "anid",
            ANumberProperty = 999,
            ADateTimeProperty = datum,
            AStringProperty = "avalue"
        };

        var result = request.GetRequestInfo();

        result.Route.Should()
            .Be(
                "/aroute/{unknown}?adatetimeproperty=2023-10-29T12%3a30%3a15Z&anumberproperty=999&astringproperty=avalue&id=anid");
        result.Method.Should().Be(OperationMethod.Get);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasUnknownPlaceholderWithDataForPost_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasUnknownPlaceholderPostRequest
        {
            Id = "anid",
            ANumberProperty = 999,
            ADateTimeProperty = datum,
            AStringProperty = "avalue"
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/{unknown}");
        result.Method.Should().Be(OperationMethod.Post);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithNullDataValuesForGet_ThenReturnsInfo()
    {
        var request = new HasPlaceholdersGetRequest
        {
            Id = null,
            AStringProperty1 = null,
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = null
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/apath1/xxxyyy/apath2/apath3");
        result.Method.Should().Be(OperationMethod.Get);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithDataValuesForGet_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersGetRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = "avalue2",
            AStringProperty3 = "avalue3",
            ANumberProperty = 999,
            ADateTimeProperty = datum
        };

        var result = request.GetRequestInfo();

        result.Route.Should()
            .Be(
                "/aroute/anid/apath1/xxx999yyy/apath2/avalue1/avalue2/apath3?adatetimeproperty=2023-10-29T12%3a30%3a15Z&astringproperty3=avalue3");
        result.Method.Should().Be(OperationMethod.Get);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Count.Should().Be(4);
        result.RouteParams["id"].Should().Be("anid");
        result.RouteParams["anumberproperty"].Should().Be(999);
        result.RouteParams["astringproperty1"].Should().Be("avalue1");
        result.RouteParams["astringproperty2"].Should().Be("avalue2");
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithSomeDataValuesForGet_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersGetRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = datum
        };

        var result = request.GetRequestInfo();

        result.Route.Should()
            .Be("/aroute/anid/apath1/xxxyyy/apath2/avalue1/apath3?adatetimeproperty=2023-10-29T12%3a30%3a15Z");
        result.Method.Should().Be(OperationMethod.Get);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Count.Should().Be(2);
        result.RouteParams["id"].Should().Be("anid");
        result.RouteParams["astringproperty1"].Should().Be("avalue1");
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithNullDataValuesForPost_ThenReturnsInfo()
    {
        var request = new HasPlaceholdersPostRequest
        {
            Id = null,
            AStringProperty1 = null,
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = null
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/apath1/xxxyyy/apath2/apath3");
        result.Method.Should().Be(OperationMethod.Post);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithDataValuesForPost_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersPostRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = "avalue2",
            AStringProperty3 = "avalue3",
            ANumberProperty = 999,
            ADateTimeProperty = datum
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/anid/apath1/xxx999yyy/apath2/avalue1/avalue2/apath3");
        result.Method.Should().Be(OperationMethod.Post);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Count.Should().Be(4);
        result.RouteParams["id"].Should().Be("anid");
        result.RouteParams["anumberproperty"].Should().Be(999);
        result.RouteParams["astringproperty1"].Should().Be("avalue1");
        result.RouteParams["astringproperty2"].Should().Be("avalue2");
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersWithSomeDataValuesForPost_ThenReturnsInfo()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersPostRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = datum
        };

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/anid/apath1/xxxyyy/apath2/avalue1/apath3");
        result.Method.Should().Be(OperationMethod.Post);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Count.Should().Be(2);
        result.RouteParams["id"].Should().Be("anid");
        result.RouteParams["astringproperty1"].Should().Be("avalue1");
    }

    [Fact]
    public void WhenGetRequestInfoForPostWithQueryParams_ThenReturnsInfo()
    {
        var request = new TestQueryStringNoPathPostRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute?aqueryproperty=aqueryvalue");
        result.Method.Should().Be(OperationMethod.Post);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersForPostWithQueryParams_ThenReturnsInfo()
    {
        var request = new TestQueryStringAndPathPostRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/apathvalue?aqueryproperty=aqueryvalue");
        result.Method.Should().Be(OperationMethod.Post);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Count.Should().Be(1);
        result.RouteParams["apathproperty"].Should().Be("apathvalue");
    }

    [Fact]
    public void WhenGetRequestInfoForPostWithQueryParamsAndBookmark_ThenReturnsInfo()
    {
        var request = new TestQueryStringAndNoPathAndBookmarkPostRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute?aqueryproperty=aqueryvalue#abookmark");
        result.Method.Should().Be(OperationMethod.Post);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRequestInfoAndRouteTemplateHasPlaceholdersForPostWithQueryParamsAndBookmark_ThenReturnsInfo()
    {
        var request = new TestQueryStringAndPathAndBookmarkPostRequest();

        var result = request.GetRequestInfo();

        result.Route.Should().Be("/aroute/apathvalue?aqueryproperty=aqueryvalue#abookmark");
        result.Method.Should().Be(OperationMethod.Post);
        result.IsTestingOnly.Should().BeFalse();
        result.RouteParams.Count.Should().Be(1);
        result.RouteParams["apathproperty"].Should().Be("apathvalue");
    }

    [Fact]
    public void WhenToUrl_ThenReturnsUrl()
    {
        var datum = new DateTime(2023, 10, 29, 12, 30, 15, DateTimeKind.Utc).ToNearestSecond();
        var request = new HasPlaceholdersPostRequest
        {
            Id = "anid",
            AStringProperty1 = "avalue1",
            AStringProperty2 = null,
            AStringProperty3 = null,
            ANumberProperty = null,
            ADateTimeProperty = datum
        };

        var result = request.ToUrl();

        result.Should().Be("/aroute/anid/apath1/xxxyyy/apath2/avalue1/apath3");
    }

    [Fact]
    public void WhenGetRouteTemplatePlaceholdersAndNoAttribute_ThenReturnsEmpty()
    {
        var result = typeof(NoRouteRequest).GetRouteTemplatePlaceholders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRouteTemplatePlaceholdersAndRequestHasNoFields_ThenReturnsEmpty()
    {
        var result = typeof(HasNoPropertiesRequest).GetRouteTemplatePlaceholders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRouteTemplatePlaceholdersAndRouteTemplateContainsNoPlaceholders_ThenReturnsEmpty()
    {
        var result = typeof(HasNoPlaceholdersPostRequest).GetRouteTemplatePlaceholders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRouteTemplatePlaceholdersAndRouteTemplateHasUnknownPlaceholder_ThenReturnsEmpty()
    {
        var result = typeof(HasUnknownPlaceholderGetRequest).GetRouteTemplatePlaceholders();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetRouteTemplatePlaceholdersAndRouteTemplateHasPlaceholdersForPost_ThenReturns()
    {
        var result = typeof(HasPlaceholdersPostRequest).GetRouteTemplatePlaceholders();

        result.Should().NotBeEmpty();
        result.Count.Should().Be(4);
        result[nameof(HasPlaceholdersPostRequest.Id)].Should().Be(typeof(string));
        result[nameof(HasPlaceholdersPostRequest.ANumberProperty)].Should().Be(typeof(int?));
        result[nameof(HasPlaceholdersPostRequest.AStringProperty1)].Should().Be(typeof(string));
        result[nameof(HasPlaceholdersPostRequest.AStringProperty2)].Should().Be(typeof(string));
    }

    [Fact]
    public void WhenSetPrivateInterHostAuthWithEmptyPostRequest_ThenSetsSignatureHeader()
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = null
        };

        message.SetPrivateInterHostAuth("asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetPrivateInterHostAuthWithEmptyPutPatchRequest_ThenSetsSignatureHeader()
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            Content = null
        };

        message.SetPrivateInterHostAuth("asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetPrivateInterHostAuthWithEmptyGetRequest_ThenSetsSignatureHeader()
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            Content = null
        };

        message.SetPrivateInterHostAuth("asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetPrivateInterHostAuthWithEmptyDeleteRequest_ThenSetsSignatureHeader()
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            Content = null
        };

        message.SetPrivateInterHostAuth("asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetPrivateInterHostAuthWithPopulatedPostRequest_ThenSetsSignatureHeader()
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = new StringContent("""
                                        {
                                          "ABodyProperty": "abodyvalue"
                                        }
                                        """)
        };
        message.SetPrivateInterHostAuth("asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should().OnlyContain(hdr =>
            hdr == "sha256=39c08f3c039a00cbe36df51b325b058daef9ce54bc9a1b21eba71c048ca68c6c");
    }

    [Fact]
    public void WhenSetPrivateInterHostAuthWithPopulatedPutPatchRequest_ThenSetsSignatureHeader()
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Put,
            Content = new StringContent("""
                                        {
                                          "ABodyProperty": "abodyvalue"
                                        }
                                        """)
        };

        message.SetPrivateInterHostAuth("asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should().OnlyContain(hdr =>
            hdr == "sha256=39c08f3c039a00cbe36df51b325b058daef9ce54bc9a1b21eba71c048ca68c6c");
    }

    [Fact]
    public void WhenSetPrivateInterHostAuthWithPopulatedGetRequest_ThenSetsSignatureHeader()
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            Content = new StringContent("""
                                        {
                                          "abodyproperty": "abodyvalue"
                                        }
                                        """)
        };

        message.SetPrivateInterHostAuth("asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    [Fact]
    public void WhenSetPrivateInterHostAuthWithPopulatedDeleteRequest_ThenSetsSignatureHeader()
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            Content = new StringContent("""
                                        {
                                          "abodyproperty": "abodyvalue"
                                        }
                                        """)
        };

        message.SetPrivateInterHostAuth("asecret");

        message.Headers.GetValues(HttpConstants.Headers.PrivateInterHostSignature).Should().OnlyContain(hdr =>
            hdr == EmptyRequestSignature);
    }

    private class NoRouteRequest : IWebRequest<TestResponse>;

    [Route("/aroute/{unknown}", OperationMethod.Get)]
    private class HasNoPropertiesRequest : IWebRequest<TestResponse>;

    [Route("/aroute", OperationMethod.Get)]
    private class HasNoPlaceholdersGetRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute", OperationMethod.Post)]
    private class HasNoPlaceholdersPostRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute/{unknown}", OperationMethod.Get)]
    private class HasUnknownPlaceholderGetRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute/{unknown}", OperationMethod.Post)]
    private class HasUnknownPlaceholderPostRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute/{id}/apath1/xxx{anumberproperty}yyy/apath2/{astringproperty1}/{astringproperty2}/apath3",
        OperationMethod.Get)]
    private class HasPlaceholdersGetRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty1 { get; set; }

        public string? AStringProperty2 { get; set; }

        public string? AStringProperty3 { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute/{id}/apath1/xxx{anumberproperty}yyy/apath2/{astringproperty1}/{astringproperty2}/apath3",
        OperationMethod.Post)]
    private class HasPlaceholdersPostRequest : IWebRequest<TestResponse>
    {
        public DateTime? ADateTimeProperty { get; set; }

        public int? ANumberProperty { get; set; }

        public string? AStringProperty1 { get; set; }

        public string? AStringProperty2 { get; set; }

        public string? AStringProperty3 { get; set; }

        public string? Id { get; set; }
    }

    [Route("/aroute/{APathProperty}", OperationMethod.Post)]
    private class TestPopulatedPostRequest : IWebRequest
    {
        public string ABodyProperty { get; set; } = "abodyvalue";

        [JsonIgnore] public string AnIgnoredProperty { get; set; } = "anignoredvalue";

        public string APathProperty { get; set; } = "apathvalue";

        [FromQuery] public string AQueryProperty { get; set; } = "aqueryvalue";
    }

    [Route("/aroute/{APathProperty}", OperationMethod.PutPatch)]
    private class TestPopulatedPutPatchRequest : IWebRequest
    {
        public string ABodyProperty { get; set; } = "abodyvalue";

        [JsonIgnore] public string AnIgnoredProperty { get; set; } = "anignoredvalue";

        public string APathProperty { get; set; } = "apathvalue";

        [FromQuery] public string AQueryProperty { get; set; } = "aqueryvalue";
    }

    [Route("/aroute/{APathProperty}", OperationMethod.Get)]
    private class TestPopulatedGetRequest : IWebRequest
    {
        public string APathProperty { get; set; } = "apathvalue";

        public string AQueryProperty { get; set; } = "aqueryvalue";
    }

    [Route("/aroute/{APathProperty}", OperationMethod.Delete)]
    private class TestPopulatedDeleteRequest : IWebRequest
    {
        public string APathProperty { get; set; } = "apathvalue";

        public string AQueryProperty { get; set; } = "aqueryvalue";
    }

    [Route("/aroute", OperationMethod.Post)]
    private class TestEmptyPostRequest : IWebRequest;

    [Route("/aroute", OperationMethod.PutPatch)]
    private class TestEmptyPutPatchRequest : IWebRequest;

    [Route("/aroute", OperationMethod.Get)]
    private class TestEmptyGetRequest : IWebRequest;

    [Route("/aroute", OperationMethod.Delete)]
    private class TestEmptyDeleteRequest : IWebRequest;

    [Route("/aroute", OperationMethod.Post)]
    private class TestQueryStringNoPathPostRequest : IWebRequest
    {
        public string ABodyProperty { get; set; } = "abodyvalue";

        [JsonIgnore] public string AnIgnoredProperty { get; set; } = "anignoredvalue";

        [FromQuery] public string AQueryProperty { get; set; } = "aqueryvalue";
    }

    [Route("/aroute#abookmark", OperationMethod.Post)]
    private class TestQueryStringAndNoPathAndBookmarkPostRequest : IWebRequest
    {
        public string ABodyProperty { get; set; } = "abodyvalue";

        [JsonIgnore] public string AnIgnoredProperty { get; set; } = "anignoredvalue";

        [FromQuery] public string AQueryProperty { get; set; } = "aqueryvalue";
    }

    [Route("/aroute/{APathProperty}", OperationMethod.Post)]
    private class TestQueryStringAndPathPostRequest : IWebRequest
    {
        public string ABodyProperty { get; set; } = "abodyvalue";

        [JsonIgnore] public string AnIgnoredProperty { get; set; } = "anignoredvalue";

        public string APathProperty { get; set; } = "apathvalue";

        [FromQuery] public string AQueryProperty { get; set; } = "aqueryvalue";
    }

    [Route("/aroute/{APathProperty}#abookmark", OperationMethod.Post)]
    private class TestQueryStringAndPathAndBookmarkPostRequest : IWebRequest
    {
        public string ABodyProperty { get; set; } = "abodyvalue";

        [JsonIgnore] public string AnIgnoredProperty { get; set; } = "anignoredvalue";

        public string APathProperty { get; set; } = "apathvalue";

        [FromQuery] public string AQueryProperty { get; set; } = "aqueryvalue";
    }
}