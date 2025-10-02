using System.Text;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[UsedImplicitly]
public class RequestTenantDetectiveSpec
{
    [Trait("Category", "Unit")]
    public class GivenAnyRequestDto
    {
        private readonly RequestTenantDetective _detective;

        public GivenAnyRequestDto()
        {
            _detective = new RequestTenantDetective();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndNoRequestType_ThenReturnsNoTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, Optional<Type>.None, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().BeNone();
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncButNoHeaderRouteQueryOrBody_ThenReturnsNoTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().BeNone();
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInHeader_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Append(HttpConstants.Headers.Tenant, "atenantid");

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAnUntenantedRequestDto
    {
        private readonly RequestTenantDetective _detective;

        public GivenAnUntenantedRequestDto()
        {
            _detective = new RequestTenantDetective();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInQueryStringAsOrganizationId_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get,
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(IUnTenantedOrganizationRequest.Id), "anid" },
                        { nameof(ITenantedRequest.OrganizationId), "anorganizationid" },
                        { nameof(RequestTenantDetective.RequestWithTenantIds.TenantId), "atenantid" }
                    })
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("anorganizationid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInQueryStringAsTenantId_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get,
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(IUnTenantedOrganizationRequest.Id), "anid" },
                        { nameof(RequestTenantDetective.RequestWithTenantIds.TenantId), "atenantid" }
                    })
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInRouteValuesAsOrganizationId_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get,
                    RouteValues = new RouteValueDictionary
                    {
                        { nameof(IUnTenantedOrganizationRequest.Id), "anid" },
                        { nameof(ITenantedRequest.OrganizationId), "anorganizationid" },
                        { nameof(RequestTenantDetective.RequestWithTenantIds.TenantId), "atenantid" }
                    }
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("anorganizationid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInRouteValuesAsTenantId_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get,
                    RouteValues = new RouteValueDictionary
                    {
                        { nameof(IUnTenantedOrganizationRequest.Id), "anid" },
                        { nameof(RequestTenantDetective.RequestWithTenantIds.TenantId), "atenantid" }
                    }
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInJsonBodyAsOrganizationId_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(new
                    {
                        Id = "anid",
                        OrganizationId = "anorganizationid",
                        TenantId = "atenantid"
                    }.ToJson()!))
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("anorganizationid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInJsonBodyAsTenantId_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(new
                    {
                        Id = "anid",
                        TenantId = "atenantid"
                    }.ToJson()!))
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInFormUrlEncodedBodyAsTenantId_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpConstants.ContentTypes.FormUrlEncoded,
                    Body = await new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new(nameof(IUnTenantedOrganizationRequest.Id), "anid"),
                        new(nameof(RequestTenantDetective.RequestWithTenantIds.TenantId), "atenantid")
                    }).ReadAsStreamAsync()
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInFormUrlEncodedBodyAsOrganizationId_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpConstants.ContentTypes.FormUrlEncoded,
                    Body = await new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new(nameof(IUnTenantedOrganizationRequest.Id), "anid"),
                        new(nameof(ITenantedRequest.OrganizationId), "anorganizationid")
                    }).ReadAsStreamAsync()
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("anorganizationid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInMultiPartFormBodyAsTenantId_ThenReturnsTenantId()
        {
            var body = new MultipartFormDataContent("aboundary");
            body.Add(new StringContent("anid"), nameof(IUnTenantedOrganizationRequest.Id));
            body.Add(new StringContent("atenantid"), nameof(RequestTenantDetective.RequestWithTenantIds.TenantId));
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = $"{HttpConstants.ContentTypes.MultiPartFormData}; boundary=\"aboundary\"",
                    Body = await body.ReadAsStreamAsync()
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInMultiPartFormBodyAsOrganizationId_ThenReturnsTenantId()
        {
            var body = new MultipartFormDataContent("aboundary");
            body.Add(new StringContent("anid"), nameof(IUnTenantedOrganizationRequest.Id));
            body.Add(new StringContent("anorganizationid"), nameof(ITenantedRequest.OrganizationId));
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = $"{HttpConstants.ContentTypes.MultiPartFormData}; boundary=\"aboundary\"",
                    Body = await body.ReadAsStreamAsync()
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("anorganizationid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenATenantedRequestDto
    {
        private readonly RequestTenantDetective _detective;

        public GivenATenantedRequestDto()
        {
            _detective = new RequestTenantDetective();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndMissingOrganizationId_ThenReturnsNoTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request = { Method = HttpMethods.Get }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().BeNone();
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInQueryString_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get,
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(ITenantedRequest.OrganizationId), "atenantid" }
                    })
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInRouteValues_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get,
                    RouteValues = new RouteValueDictionary
                    {
                        { nameof(ITenantedRequest.OrganizationId), "atenantid" }
                    }
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInJsonBody_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(new
                    {
                        OrganizationId = "atenantid"
                    }.ToJson()!))
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInFormUrlEncodedBody_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpConstants.ContentTypes.FormUrlEncoded,
                    Body = await new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new(nameof(ITenantedRequest.OrganizationId), "anorganizationid")
                    }).ReadAsStreamAsync()
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("anorganizationid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInMultiPartFormBody_ThenReturnsTenantId()
        {
            var body = new MultipartFormDataContent("aboundary");
            body.Add(new StringContent("anorganizationid"), nameof(ITenantedRequest.OrganizationId));
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = $"{HttpConstants.ContentTypes.MultiPartFormData}; boundary=\"aboundary\"",
                    Body = await body.ReadAsStreamAsync()
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("anorganizationid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenATenantedOrganizationRequestDto
    {
        private readonly RequestTenantDetective _detective;

        public GivenATenantedOrganizationRequestDto()
        {
            _detective = new RequestTenantDetective();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndMissingId_ThenReturnsNoTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request = { Method = HttpMethods.Get }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedOrganizationRequest),
                    CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().BeNone();
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInQueryString_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get,
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(IUnTenantedOrganizationRequest.Id), "atenantid" }
                    })
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedOrganizationRequest),
                    CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInRouteValues_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get,
                    RouteValues = new RouteValueDictionary
                    {
                        { nameof(IUnTenantedOrganizationRequest.Id), "atenantid" }
                    }
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedOrganizationRequest),
                    CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInJsonBody_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(new
                    {
                        Id = "atenantid"
                    }.ToJson()!))
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedOrganizationRequest),
                    CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInFormUrlEncodedBody_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpConstants.ContentTypes.FormUrlEncoded,
                    Body = await new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new(nameof(IUnTenantedOrganizationRequest.Id), "atenantid")
                    }).ReadAsStreamAsync()
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedOrganizationRequest),
                    CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInMultiPartFormBody_ThenReturnsTenantId()
        {
            var body = new MultipartFormDataContent("aboundary");
            body.Add(new StringContent("atenantid"), nameof(IUnTenantedOrganizationRequest.Id));
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = $"{HttpConstants.ContentTypes.MultiPartFormData}; boundary=\"aboundary\"",
                    Body = await body.ReadAsStreamAsync()
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedOrganizationRequest),
                    CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }
    }

    [UsedImplicitly]
    public class TestUnTenantedRequest : UnTenantedRequest<TestUnTenantedRequest, TestResponse>;

    [UsedImplicitly]
    public class TestTenantedRequest : TenantedRequest<TestTenantedRequest, TestResponse>;

    [UsedImplicitly]
    public class TestUnTenantedOrganizationRequest : UnTenantedRequest<TestUnTenantedOrganizationRequest, TestResponse>,
        IUnTenantedOrganizationRequest
    {
        public string? Id { get; set; }
    }
}