using FluentAssertions;
using HtmlAgilityPack;
using Infrastructure.Hosting.Common;
using Infrastructure.Web.Interfaces;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.Website.Common;
using Xunit;

namespace WebsiteHost.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("WEBSITE")]
public class ApiDocsSpec : WebsiteSpec<Program, ApiHost1.Program>
{
    public ApiDocsSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenGetSwaggerUI_ThenDisplayed()
    {
        var result = await HttpApi.GetAsync($"{WebConstants.BackEndForFrontEndDocsPath}/index.html");

        var content = await result.Content.ReadAsStringAsync();
        content.Should().Contain("<html");

        var doc = new HtmlDocument();
        doc.Load(await result.Content.ReadAsStreamAsync());

        var swaggerPage = doc.DocumentNode
            .SelectSingleNode("//div[@id='swagger-ui']");

        swaggerPage.Should().NotBeNull();

        var title = doc.DocumentNode.SelectSingleNode("//title");

        title.Should().NotBeNull();
        title!.InnerText.Should().Be(HostOptions.BackEndForFrontEndWebHost.HostName);
    }
}