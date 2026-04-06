using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class DictionaryExtensionsSpec
{
    [Fact]
    public void WhenMergeAndSourceAndOtherIsEmpty_ThenContainsNothing()
    {
        var source = new Dictionary<string, string>();

        source.Merge(new Dictionary<string, string>());

        source.Count.Should().Be(0);
    }

    [Fact]
    public void WhenMergeAndOtherIsEmpty_ThenNothingAdded()
    {
        var source = new Dictionary<string, string>
        {
            { "aname", "avalue" }
        };

        source.Merge(new Dictionary<string, string>());

        source.Count.Should().Be(1);
        source.Should().OnlyContain(pair => pair.Key == "aname" && pair.Value == "avalue");
    }

    [Fact]
    public void WhenMergeAndSourceIsEmpty_ThenOtherAdded()
    {
        var source = new Dictionary<string, string>();

        source.Merge(new Dictionary<string, string>
        {
            { "aname", "avalue" }
        });

        source.Count.Should().Be(1);
        source.Should().OnlyContain(pair => pair.Key == "aname" && pair.Value == "avalue");
    }

    [Fact]
    public void WhenMergeAndSourceAndOtherHaveUniqueKeys_ThenOtherAdded()
    {
        var source = new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        };

        source.Merge(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });

        source.Count.Should().Be(2);
        source.Should().Contain(pair => pair.Key == "aname1" && pair.Value == "avalue1");
        source.Should().Contain(pair => pair.Key == "aname2" && pair.Value == "avalue2");
    }

    [Fact]
    public void WhenMergeAndSourceAndOtherHaveSameKeys_ThenSourceRemains()
    {
        var source = new Dictionary<string, string>
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" }
        };

        source.Merge(new Dictionary<string, string>
        {
            { "aname2", "avalue4" },
            { "aname3", "avalue3" }
        });

        source.Count.Should().Be(3);
        source.Should().Contain(pair => pair.Key == "aname1" && pair.Value == "avalue1");
        source.Should().Contain(pair => pair.Key == "aname2" && pair.Value == "avalue4");
        source.Should().Contain(pair => pair.Key == "aname3" && pair.Value == "avalue3");
    }

    [Fact]
    public void WhenToStringDictionaryWithNullInstance_ThenreturnsEmpty()
    {
        var result = ((TestMappingClass?)null).ToStringDictionary();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenToStringDictionaryWithInstanceWithValues_ThenReturnsProperties()
    {
        var datum = DateTime.UtcNow;
        var result = new TestMappingClass
        {
            ADateTimeProperty = datum,
            AnOptionalStringProperty = "avalue",
            AnOptionalNullableStringProperty = "avalue",
            AnOptionalDateTimeProperty = datum,
            AnOptionalNullableDateTimeProperty = datum
        }.ToStringDictionary();

        result.Count.Should().Be(8);
        result[nameof(TestMappingClass.AStringProperty)].Should().Be("adefaultvalue");
        result[nameof(TestMappingClass.AnIntProperty)].Should().Be("1");
        result[nameof(TestMappingClass.AnBoolProperty)].Should().Be("True");
        result[nameof(TestMappingClass.ADateTimeProperty)].Should().Be(datum.ToIso8601());
        result[nameof(TestMappingClass.AnOptionalStringProperty)].Should().Be("avalue");
        result[nameof(TestMappingClass.AnOptionalNullableStringProperty)].Should().Be("avalue");
        result[nameof(TestMappingClass.AnOptionalDateTimeProperty)].Should().Be(datum.ToIso8601());
        result[nameof(TestMappingClass.AnOptionalNullableDateTimeProperty)].Should().Be(datum.ToIso8601());
    }

    [Fact]
    public void WhenToStringDictionaryWithInstanceWithDefaultValues_ThenReturnsProperties()
    {
        var result = new TestMappingClass
        {
            AStringProperty = default!,
            AnIntProperty = default,
            AnBoolProperty = default,
            ADateTimeProperty = default,
            AnOptionalStringProperty = default,
            AnOptionalNullableStringProperty = default,
            AnOptionalDateTimeProperty = default,
            AnOptionalNullableDateTimeProperty = default
        }.ToStringDictionary();

        result.Count.Should().Be(3);
        result[nameof(TestMappingClass.AnIntProperty)].Should().Be("0");
        result[nameof(TestMappingClass.AnBoolProperty)].Should().Be("False");
        result[nameof(TestMappingClass.ADateTimeProperty)].Should().Be("0001-01-01T00:00:00");
    }
}