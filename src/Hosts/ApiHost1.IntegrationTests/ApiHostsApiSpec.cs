using FluentAssertions;
using Infrastructure.Hosting.Common;
using Infrastructure.Web.Api.Operations.Shared.ApiHosts;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace ApiHost1.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class ApiHostsApiSpec : WebApiSpec<Program>
{
    public ApiHostsApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenCheckHealth_ThenStatusOK()
    {
        var result = await Api.GetAsync(new ApiHealthCheckRequest());

        result.Content.Value.Health.Name.Should().Be(HostOptions.BackEndAncillaryApiHost.HostName);
        result.Content.Value.Health.Status.Should().Be("OK");
    }

    [Fact]
    public async Task WhenFetchStatistics_ThenCounts()
    {
        var result = await Api.GetAsync(new ApiStatisticsRequest());

        result.Content.Value.Statistics.Name.Should().Be(HostOptions.BackEndAncillaryApiHost.HostName);
        result.Content.Value.Statistics.Total.Should().BeGreaterThan(1);
    }
}