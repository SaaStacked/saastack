using Common.Extensions;
using FluentAssertions;
using JetBrains.Annotations;
using UnitTesting.Common.Validation;
using Xunit;

namespace Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class MappingExtensionsSpec
{
    [Fact]
    public void WhenFromObjectDictionaryWithEmptyInstance_ThenReturnsDefaultInstance()
    {
        var result = new Dictionary<string, object?>().AsReadOnly()
            .FromObjectDictionary<TestMappingClass>();

        result.AStringProperty.Should().Be("adefaultvalue");
        result.AnIntProperty.Should().Be(1);
        result.AnBoolProperty.Should().Be(true);
        result.ADateTimeProperty.Should().BeNear(DateTime.UtcNow);
        result.AnOptionalStringProperty.Should().Be(Optional<string>.None);
        result.AnOptionalNullableStringProperty.Should().Be(Optional<string?>.None);
        result.AnOptionalDateTimeProperty.Should().Be(Optional<DateTime>.None);
        result.AnOptionalNullableDateTimeProperty.Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenFromObjectDictionaryWithNonMatchingProperties_ThenReturnsDefaultInstance()
    {
        var result = new Dictionary<string, object?>
            {
                { "anunknownproperty", "avalue" }
            }.AsReadOnly()
            .FromObjectDictionary<TestMappingClass>();

        result.AStringProperty.Should().Be("adefaultvalue");
        result.AnIntProperty.Should().Be(1);
        result.AnBoolProperty.Should().Be(true);
        result.ADateTimeProperty.Should().BeNear(DateTime.UtcNow);
        result.AnOptionalStringProperty.Should().Be(Optional<string>.None);
        result.AnOptionalNullableStringProperty.Should().Be(Optional<string?>.None);
        result.AnOptionalDateTimeProperty.Should().Be(Optional<DateTime>.None);
        result.AnOptionalNullableDateTimeProperty.Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenFromObjectDictionaryWithMatchingProperties_ThenReturnsUpdatedInstance()
    {
        var datum = DateTime.Today;
        var result = new Dictionary<string, object?>
            {
                { nameof(TestMappingClass.AStringProperty), "avalue" },
                { nameof(TestMappingClass.AnIntProperty), 99 },
                { nameof(TestMappingClass.AnBoolProperty), false },
                { nameof(TestMappingClass.ADateTimeProperty), datum },
                { nameof(TestMappingClass.AnOptionalStringProperty), "avalue" },
                { nameof(TestMappingClass.AnOptionalNullableStringProperty), "avalue" },
                { nameof(TestMappingClass.AnOptionalDateTimeProperty), datum },
                { nameof(TestMappingClass.AnOptionalNullableDateTimeProperty), datum }
            }.AsReadOnly()
            .FromObjectDictionary<TestMappingClass>();

        result.AStringProperty.Should().Be("avalue");
        result.AnIntProperty.Should().Be(99);
        result.AnBoolProperty.Should().Be(false);
        result.ADateTimeProperty.Should().Be(datum);
        result.AnOptionalStringProperty.Should().Be("avalue");
        result.AnOptionalNullableStringProperty.Should().Be("avalue");
        result.AnOptionalDateTimeProperty.Should().Be(datum);
        result.AnOptionalNullableDateTimeProperty.Should().Be(datum);
    }

    [Fact]
    public void WhenFromObjectDictionaryWithCamelCasedProperties_ThenReturnsUpdatedInstance()
    {
        var datum = DateTime.Today;
        var result = new Dictionary<string, object?>
            {
                { "aStringProperty", "avalue" },
                { "anIntProperty", 99 },
                { "anBoolProperty", false },
                { "aDateTimeProperty", datum }
            }.AsReadOnly()
            .FromObjectDictionary<TestMappingClass>();

        result.AStringProperty.Should().Be("avalue");
        result.AnIntProperty.Should().Be(99);
        result.AnBoolProperty.Should().Be(false);
        result.ADateTimeProperty.Should().Be(datum);
    }

    [Fact]
    public void WhenFromObjectDictionaryWithMatchingNullProperties_ThenReturnsUpdatedInstanceWithDefaultValues()
    {
        var result = new Dictionary<string, object?>
            {
                { nameof(TestMappingClass.AStringProperty), null },
                { nameof(TestMappingClass.AnIntProperty), null },
                { nameof(TestMappingClass.AnBoolProperty), null },
                { nameof(TestMappingClass.ADateTimeProperty), null },
                { nameof(TestMappingClass.AnOptionalStringProperty), null },
                { nameof(TestMappingClass.AnOptionalNullableStringProperty), null },
                { nameof(TestMappingClass.AnOptionalDateTimeProperty), null },
                { nameof(TestMappingClass.AnOptionalNullableDateTimeProperty), null }
            }.AsReadOnly()
            .FromObjectDictionary<TestMappingClass>();

        result.AStringProperty.Should().BeNull();
        result.AnIntProperty.Should().Be(default);
        result.AnBoolProperty.Should().Be(default);
        result.ADateTimeProperty.Should().Be(default);
        result.AnOptionalStringProperty.Should().Be(Optional<string>.None);
        result.AnOptionalNullableStringProperty.Should().Be(Optional<string?>.None);
        result.AnOptionalDateTimeProperty.Should().Be(Optional<DateTime>.None);
        result.AnOptionalNullableDateTimeProperty.Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenFromObjectDictionaryWithConstructorBoundProperties_ThenReturnsUpdatedInstance()
    {
        var result = new Dictionary<string, object?>
            {
                { nameof(TestConstructorMappingClass.AStringProperty), "avalue" },
                { nameof(TestConstructorMappingClass.AnIntProperty), 99 }
            }.AsReadOnly()
            .FromObjectDictionary<TestConstructorMappingClass>();

        result.AStringProperty.Should().Be("avalue");
        result.AnIntProperty.Should().Be(99);
    }

    [Fact]
    public void WhenFromObjectDictionaryWithConstructorBoundNullProperties_ThenReturnsUpdatedInstanceWithDefaultValues()
    {
        var result = new Dictionary<string, object?>
            {
                { nameof(TestConstructorMappingClass.AStringProperty), null },
                { nameof(TestConstructorMappingClass.AnIntProperty), null }
            }.AsReadOnly()
            .FromObjectDictionary<TestConstructorMappingClass>();

        result.AStringProperty.Should().BeNull();
        result.AnIntProperty.Should().Be(default);
    }

    [Fact]
    public void WhenToObjectDictionaryWithNullInstance_ThenReturnsEmpty()
    {
        var result = ((TestMappingClass?)null).ToObjectDictionary();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenToObjectDictionaryWithInstanceWithValues_ThenReturnsProperties()
    {
        var result = new TestMappingClass().ToObjectDictionary();

        result.Count.Should().Be(8);
        result[nameof(TestMappingClass.AStringProperty)].Should().Be("adefaultvalue");
        result[nameof(TestMappingClass.AnIntProperty)].Should().Be(1);
        result[nameof(TestMappingClass.AnBoolProperty)].Should().Be(true);
        result[nameof(TestMappingClass.ADateTimeProperty)].As<DateTime>().Should().BeNear(DateTime.UtcNow);
        result[nameof(TestMappingClass.AnOptionalStringProperty)].Should().Be(Optional<string>.None);
        result[nameof(TestMappingClass.AnOptionalNullableStringProperty)].Should().Be(Optional<string?>.None);
        result[nameof(TestMappingClass.AnOptionalDateTimeProperty)].Should().Be(Optional<DateTime>.None);
        result[nameof(TestMappingClass.AnOptionalNullableDateTimeProperty)].Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenToObjectDictionaryWithInstanceWithDefaultValues_ThenReturnsProperties()
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
        }.ToObjectDictionary();

        result.Count.Should().Be(8);
        result[nameof(TestMappingClass.AStringProperty)].Should().BeNull();
        result[nameof(TestMappingClass.AnIntProperty)].Should().Be(default(int));
        result[nameof(TestMappingClass.AnBoolProperty)].Should().Be(default(bool));
        result[nameof(TestMappingClass.ADateTimeProperty)].Should().Be(default(DateTime));
        result[nameof(TestMappingClass.AnOptionalStringProperty)].Should().Be(Optional<string>.None);
        result[nameof(TestMappingClass.AnOptionalNullableStringProperty)].Should().Be(Optional<string?>.None);
        result[nameof(TestMappingClass.AnOptionalDateTimeProperty)].Should().Be(Optional<DateTime>.None);
        result[nameof(TestMappingClass.AnOptionalNullableDateTimeProperty)].Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenToObjectDictionaryWithDictionaryInstance_ThenReturnsEntries()
    {
        var result = new Dictionary<string, string>
        {
            { "aname", "avalue" },
            { "anothername", "anothervalue" }
        }.ToObjectDictionary();

        result.Count.Should().Be(2);
        result["aname"].Should().Be("avalue");
        result["anothername"].Should().Be("anothervalue");
    }

    [Fact]
    public void WhenConvertWithMatchingProperties_ThenReturnsMappedInstance()
    {
        var result = new TestConvertSource
        {
            AStringProperty = "avalue",
            AnIntProperty = 99
        }.Convert<TestConvertSource, TestConvertTarget>();

        result.AStringProperty.Should().Be("avalue");
        result.AnIntProperty.Should().Be(99);
    }
}

[UsedImplicitly]
public class TestMappingClass
{
    public DateTime ADateTimeProperty { get; set; } = DateTime.UtcNow;

    public bool AnBoolProperty { get; set; } = true;

    public int AnIntProperty { get; set; } = 1;

    public Optional<DateTime> AnOptionalDateTimeProperty { get; set; }

    public Optional<DateTime?> AnOptionalNullableDateTimeProperty { get; set; }

    public Optional<string?> AnOptionalNullableStringProperty { get; set; }

    public Optional<string> AnOptionalStringProperty { get; set; }

    public string AStringProperty { get; set; } = "adefaultvalue";
}

[UsedImplicitly]
public class TestConstructorMappingClass(string aStringProperty, int anIntProperty)
{
    public string AStringProperty { get; } = aStringProperty;

    public int AnIntProperty { get; } = anIntProperty;
}

[UsedImplicitly]
public class TestConvertSource
{
    public int AnIntProperty { get; set; }

    public string AStringProperty { get; set; } = string.Empty;
}

[UsedImplicitly]
public class TestConvertTarget(string aStringProperty, int anIntProperty)
{
    public string AStringProperty { get; } = aStringProperty;

    public int AnIntProperty { get; } = anIntProperty;
}