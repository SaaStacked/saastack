using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure.Hosting.Common;
using Infrastructure.Web.Api.Operations.Shared.ApiHosts;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.Website.Common;
using Xunit;

namespace WebsiteHost.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("WEBSITE")]
public class ApiHostApiSpec : WebsiteSpec<Program, ApiHost1.Program>
{
    public ApiHostApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenCheck_ThenStatusOK()
    {
        var result = await HttpApi.GetAsync(new ApiHealthCheckRequest().MakeApiRoute());

        var content = await result.Content.ReadFromJsonAsync<ApiHealthCheckResponse>();
        content!.Health.Name.Should().Be(HostOptions.BackEndForFrontEndWebHost.HostName);
        content.Health.Status.Should().Be("OK");
    }

    [Fact]
    public async Task WhenCheckHealth_ThenStatusOK()
    {
        var result = await HttpApi.GetAsync(new ApiHealthCheckRequest().MakeApiRoute());

        var content = await result.Content.ReadFromJsonAsync<ApiHealthCheckResponse>();
        content!.Health.Name.Should().Be(HostOptions.BackEndForFrontEndWebHost.HostName);
        content.Health.Status.Should().Be("OK");
    }

    [Fact]
    public async Task WhenFetchStatistics_ThenCounts()
    {
        var result = await HttpApi.GetAsync(new ApiStatisticsRequest().MakeApiRoute());

        var content = await result.Content.ReadFromJsonAsync<ApiStatisticsResponse>();

        content!.Statistics.Name.Should().Be(HostOptions.BackEndForFrontEndWebHost.HostName);
        content.Statistics.Total.Should().BeGreaterThan(1);
    }
}