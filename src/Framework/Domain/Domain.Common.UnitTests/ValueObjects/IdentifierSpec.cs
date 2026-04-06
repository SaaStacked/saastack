using Common.Extensions;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class IdentifierSpec
{
    [Fact]
    public void WhenConvertIdentifier_ThenMapsToStringValue()
    {
        var @object = new TestObject
        {
            StringValue = Identifier.Create("avalue")
        };

        var result = @object.Convert<TestObject, TestDto>();

        result.StringValue.Should().Be("avalue");
    }
}

public class TestObject
{
    public Identifier? StringValue { get; set; }
}

public class TestDto
{
    public string? StringValue { get; set; }
}