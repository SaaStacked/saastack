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
}