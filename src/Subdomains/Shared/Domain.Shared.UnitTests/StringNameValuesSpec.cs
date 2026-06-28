using Common;
using Common.Extensions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class StringNameValuesSpec
{
    [Fact]
    public void WhenEmpty_ThenEmpty()
    {
        var result = StringNameValues.Empty;

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithItems_ThenReturnsSuccess()
    {
        var result = StringNameValues.Create(new Dictionary<string, string> { { "aname", "avalue" } });

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(x => x.Key == "aname" && x.Value == "avalue");
    }

    [Fact]
    public void WhenCreateWithItemWithNullValue_ThenReturnsError()
    {
        var result = StringNameValues.Create(new Dictionary<string, string> { { "aname", null! } });

        result.Should().BeError(ErrorCode.Validation, Resources.StringNameValues_InvalidValue.Format("aname"));
    }

    [Fact]
    public void WhenAppendAndEmpty_ThenAppends()
    {
        var result = StringNameValues.Empty.Append("aname", "avalue");

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(x => x.Key == "aname" && x.Value == "avalue");
    }

    [Fact]
    public void WhenAppendAndExists_ThenUpdatesValue()
    {
        var values = StringNameValues.Create(new Dictionary<string, string> { { "aname", "avalue1" } }).Value;

        var result = values.Append("aname", "avalue2");

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(x => x.Key == "aname" && x.Value == "avalue2");
    }

    [Fact]
    public void WhenAppendAndNotExists_ThenAppendsValue()
    {
        var values = StringNameValues.Create(new Dictionary<string, string> { { "aname1", "avalue1" } }).Value;

        var result = values.Append("aname2", "avalue2");

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(2);
        result.Value.Items.Should().Contain(x => x.Key == "aname1" && x.Value == "avalue1");
        result.Value.Items.Should().Contain(x => x.Key == "aname2" && x.Value == "avalue2");
    }
}