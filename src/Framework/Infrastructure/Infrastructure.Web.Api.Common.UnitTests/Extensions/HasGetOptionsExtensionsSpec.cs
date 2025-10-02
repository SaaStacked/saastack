using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class HasGetOptionsExtensionsSpec
{
    private readonly GetOptionsDto _hasGetOptions;

    public HasGetOptionsExtensionsSpec()
    {
        _hasGetOptions = new GetOptionsDto();
    }

    [Fact]
    public void WhenToGetOptionsAndNullOptions_ThenReturnsNull()
    {
        var result = ((GetOptionsDto)null!).ToGetOptions();

        result.Should().BeEquivalentTo(new GetOptions());
    }

    [Fact]
    public void WhenToGetOptionsAndEmbedIsUndefinedAndIsSearchOptions_ThenReturnsExpandNone()
    {
        var searchOptions = new SearchOptionsDto
        {
            Embed = null!
        };

        var result = searchOptions.ToGetOptions();

        result.Expand.Should().Be(ExpandOptions.None);
        result.ResourceReferences.Count().Should().Be(0);
    }

    [Fact]
    public void WhenToGetOptionsAndEmbedIsUndefined_ThenReturnsExpandAll()
    {
        _hasGetOptions.Embed = null!;

        var result = _hasGetOptions.ToGetOptions();

        result.Expand.Should().Be(ExpandOptions.All);
        result.ResourceReferences.Count().Should().Be(0);
    }

    [Fact]
    public void WhenToGetOptionsAndEmbedIsOff_ThenReturnsDisabled()
    {
        _hasGetOptions.Embed = HasGetOptions.EmbedNone;

        var result = _hasGetOptions.ToGetOptions();

        result.Expand.Should().Be(ExpandOptions.None);
        result.ResourceReferences.Count()
            .Should().Be(0);
    }

    [Fact]
    public void WhenToGetOptionsAndEmbedIsAll_ThenReturnsEnabled()
    {
        _hasGetOptions.Embed = HasGetOptions.EmbedAll;

        var result = _hasGetOptions.ToGetOptions();

        result.Expand.Should().Be(ExpandOptions.All);
        result.ResourceReferences.Count()
            .Should().Be(0);
    }

    [Fact]
    public void WhenToGetOptionsAndEmbedIsCommaDelimitedResourceReferences_ThenReturnsChildResources()
    {
        _hasGetOptions.Embed = "aresourceref1, aresourceref2, aresourceref3,,,";

        var result = _hasGetOptions.ToGetOptions();

        result.Expand.Should().Be(ExpandOptions.Custom);
        result.ResourceReferences.Count()
            .Should().Be(3);
        result.ResourceReferences.ToList()[0]
            .Should().Be("aresourceref1");
        result.ResourceReferences.ToList()[1]
            .Should().Be("aresourceref2");
        result.ResourceReferences.ToList()[2]
            .Should().Be("aresourceref3");
    }

    [Fact]
    public void WhenToEmbedWithNothing_ThenReturns()
    {
        var result = new GetOptions().ToEmbed();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToEmbedWithAll_ThenReturns()
    {
        var result = new GetOptions(ExpandOptions.All)
            .ToEmbed();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToEmbedWithNone_ThenReturns()
    {
        var result = new GetOptions(ExpandOptions.None)
            .ToEmbed();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToEmbedWithCustomButNoReferences_ThenReturns()
    {
        var result = new GetOptions(ExpandOptions.Custom, [])
            .ToEmbed();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToEmbedWithCustomWithReferences_ThenReturns()
    {
        var result = new GetOptions(ExpandOptions.Custom, ["aresource1", "aresource2"])
            .ToEmbed();

        result.Should().Be("aresource1,aresource2");
    }
}