#if TESTINGONLY
using System.Net;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Interfaces;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Xunit;

namespace ApiHost1.IntegrationTests;

[UsedImplicitly]
public class RequestCorrelationApiSpec
{
    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAnHttpClient : WebApiSpec<Program>
    {
        public GivenAnHttpClient(WebApiSetup<Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenGetWithNoRequestId_ThenReturnsGeneratedResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpConstants.Headers.RequestId).FirstOrDefault().Should()
                .NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenGetWithRequestId_ThenReturnsSameResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest(),
                message => { message.Headers.Add("Request-ID", "acorrelationid"); });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpConstants.Headers.RequestId).FirstOrDefault().Should()
                .Be("acorrelationid");
        }

        [Fact]
        public async Task WhenGetWithXRequestId_ThenReturnsSameResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest(),
                message => { message.Headers.Add("X-Request-ID", "acorrelationid"); });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpConstants.Headers.RequestId).FirstOrDefault().Should()
                .Be("acorrelationid");
        }

        [Fact]
        public async Task WhenGetWithXCorrelationId_ThenReturnsSameResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest(),
                message => { message.Headers.Add("X-Correlation-ID", "acorrelationid"); });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpConstants.Headers.RequestId).FirstOrDefault().Should()
                .Be("acorrelationid");
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAJsonClient : WebApiSpec<Program>
    {
        public GivenAJsonClient(WebApiSetup<Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenGetWithNoRequestId_ThenReturnsGeneratedResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.RequestId.Should().NotBeNullOrEmpty();
        }
    }
}
#endif