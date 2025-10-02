using FluentAssertions;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Endpoints;

[Trait("Category", "Unit")]
public class RequestCorrelationFilterSpec
{
    private readonly RequestCorrelationFilter _filter;

    public RequestCorrelationFilterSpec()
    {
        _filter = new RequestCorrelationFilter();
    }

    [Fact]
    public async Task WhenInvokeAsyncAndNotInRequestPipelineAndNotInHeaders_ThenFabricatesNew()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        await _filter.InvokeAsync(context, next);

        context.HttpContext.Items[RequestCorrelationFilter.CorrelationIdItemName]
            .Should().NotBeNull();
        context.HttpContext.Response.Headers[RequestCorrelationFilter.ResponseHeaderName]
            .Should().NotBeNullOrEmpty();
        context.HttpContext.Items[RequestCorrelationFilter.CorrelationIdItemName]
            .Should().Be(context.HttpContext.Response.Headers[HttpConstants.Headers.RequestId].First());
    }

    [Fact]
    public async Task WhenInvokeAsyncAndInRequestPipeline_ThenUses()
    {
        var httpContext = new DefaultHttpContext
        {
            Items =
            {
                [RequestCorrelationFilter.CorrelationIdItemName] = "acorrelationid"
            }
        };
        var context = new DefaultEndpointFilterInvocationContext(httpContext);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        await _filter.InvokeAsync(context, next);

        context.HttpContext.Items[RequestCorrelationFilter.CorrelationIdItemName]
            .Should().Be("acorrelationid");
        context.HttpContext.Response.Headers[HttpConstants.Headers.RequestId].First()
            .Should().Be("acorrelationid");
    }

    [Fact]
    public async Task WhenInvokeAsyncAndInAnAcceptedRequestHeader_ThenUses()
    {
        var acceptedHeader = RequestCorrelationFilter.AcceptedRequestHeaderNames[0];
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[acceptedHeader] = "acorrelationid";
        var context = new DefaultEndpointFilterInvocationContext(httpContext);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        await _filter.InvokeAsync(context, next);

        context.HttpContext.Items[RequestCorrelationFilter.CorrelationIdItemName]
            .Should().Be("acorrelationid");
        context.HttpContext.Response.Headers[HttpConstants.Headers.RequestId].First()
            .Should().Be("acorrelationid");
    }
}