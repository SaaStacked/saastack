using Common;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.External.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using QueryAny;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.External.Persistence.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class StoreExtensionsSpec
{
    [Fact]
    public void WhenComplexTypeFromContainerPropertyWithNone_ThenReturnsNone()
    {
        var result = Optional<string>.None
            .ComplexTypeFromContainerProperty(typeof(string));

        result.Should().BeNone();
    }

    [Fact]
    public void WhenComplexTypeFromContainerPropertyWithEmpty_ThenReturnsNone()
    {
        var result = string.Empty.ToOptional()
            .ComplexTypeFromContainerProperty(typeof(string));

        result.Should().BeNone();
    }

    [Fact]
    public void WhenComplexTypeFromContainerPropertyAndTargetTypeIsNotComplexType_ThenReturnsSome()
    {
        var result = "avalue".ToOptional()
            .ComplexTypeFromContainerProperty(typeof(string));

        result.Should().BeSome("avalue");
    }

    [Fact]
    public void WhenComplexTypeFromContainerPropertyAndTargetTypeIsComplexTypeAndValueIsNotJson_ThenReturnsNone()
    {
        var result = "{notvalidjson}".ToOptional()
            .ComplexTypeFromContainerProperty(typeof(TestComplexType));

        result.Should().BeNone();
    }

    [Fact]
    public void
        WhenComplexTypeFromContainerPropertyAndTargetTypeIsComplexTypeAndValueIsEmptyJson_ThenReturnsDefaultInstance()
    {
        var result = "{}".ToOptional()
            .ComplexTypeFromContainerProperty(typeof(TestComplexType));

        result.Value.Should().BeEquivalentTo(new TestComplexType());
    }

    [Fact]
    public void WhenComplexTypeFromContainerPropertyAndTargetTypeIsComplexTypeAndValueIsJsonValue_ThenReturnsSome()
    {
        var result = $"{{\"{nameof(TestComplexType.AProperty)}\":\"avalue\"}}".ToOptional()
            .ComplexTypeFromContainerProperty(typeof(TestComplexType));

        result.Value.Should().BeEquivalentTo(new TestComplexType { AProperty = "avalue" });
    }

    [Fact]
    public void WhenComplexTypeToContainerPropertyWithNone_ThenReturnsNone()
    {
        var result = Optional<string>.None
            .ComplexTypeToContainerProperty();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenComplexTypeToContainerPropertyWithAnEmptyStringValue_ThenReturnsStringValue()
    {
        var result = string.Empty.ToOptional()
            .ComplexTypeToContainerProperty();

        result.Should().BeSome(string.Empty);
    }

    [Fact]
    public void WhenComplexTypeToContainerPropertyWithASupportedPrimitiveValue_ThenReturnsStringValue()
    {
        var result = 99.ToOptional()
            .ComplexTypeToContainerProperty();

        result.Should().BeSome("99");
    }

    [Fact]
    public void
        WhenComplexTypeToContainerPropertyWithAComplexTypeWithOverwrittenToStringMethodValue_ThenReturnsToStringValue()
    {
        var result = new TestComplexTypeWithOverwrittenToString { AProperty = "avalue" }.ToOptional()
            .ComplexTypeToContainerProperty();

        result.Should().BeSome("overwritten");
    }

    [Fact]
    public void WhenComplexTypeToContainerPropertyWithAComplexTypeValue_ThenReturnsStringifiedValue()
    {
        var result = new TestComplexType { AProperty = "avalue" }.ToOptional()
            .ComplexTypeToContainerProperty();

        result.Should().BeSome($"{{\"{nameof(TestComplexType.AProperty)}\":\"avalue\"}}");
    }

    [Fact]
    public async Task WhenFetchAllIntoMemoryAsync_ThenReturnsResults()
    {
        var query = Query.From<TestDto>().WhereAll();
        var dtoProperties = HydrationProperties.FromDto(new TestDto { Id = "anid" });
        var metadata = PersistedEntityMetadata.FromType<TestDto>();
        var primaryEntities = new Dictionary<string, HydrationProperties>
        {
            { "anid", dtoProperties }
        };
        var joinedEntities = new Dictionary<string, HydrationProperties>();

        var result =
            await query.FetchAllIntoMemoryAsync(10, metadata,
                () => Task.FromResult(primaryEntities),
                _ => Task.FromResult(joinedEntities));

        result.TotalCount.Should().Be(1);
        result.Results.Count.Should().Be(1);
        result.Results[0].Id.Should().Be("anid");
    }

    [Fact]
    public void WhenGetDefaultOrderingForEntityWithoutAnyReadModelsFields_ThenReturnsLastSchemaColumn()
    {
        var query = Query.From<TestQueryEntityWithoutAnyReadModelFields>().WhereAll();
        var metadata = PersistedEntityMetadata.FromType<TestQueryEntityWithoutAnyReadModelFields>();

        var result = query.GetDefaultOrdering(metadata);

        result.Should().Be(nameof(TestQueryEntityWithoutAnyReadModelFields.AProperty1));
    }

    [Fact]
    public void WhenGetDefaultOrderingForEntityWithOnlyId_ThenReturnsIdColumn()
    {
        var query = Query.From<TestQueryEntityWithIdOnly>().WhereAll();
        var metadata = PersistedEntityMetadata.FromType<TestQueryEntityWithIdOnly>();

        var result = query.GetDefaultOrdering(metadata);

        result.Should().Be(nameof(TestQueryEntityWithIdOnly.Id));
    }

    [Fact]
    public void WhenGetDefaultOrderingForEntityWithAllReadModelFields_ThenReturnsLastPersistedAtColumn()
    {
        var query = Query.From<TestQueryEntityWithReadModelFields>().WhereAll();
        var metadata = PersistedEntityMetadata.FromType<TestQueryEntityWithReadModelFields>();

        var result = query.GetDefaultOrdering(metadata);

        result.Should().Be(nameof(TestQueryEntityWithReadModelFields.LastPersistedAt));
    }    
    
    [Fact]
    public void
        WhenGetDefaultOrderingForEntityWithoutAnyReadModelFieldsButSelectsAColumn_ThenReturnsFirstSelectedColumn()
    {
        var query = Query.From<TestQueryEntityWithoutAnyReadModelFields>()
            .WhereAll()
            .Select(x => x.AProperty2);
        var metadata = PersistedEntityMetadata.FromType<TestQueryEntityWithoutAnyReadModelFields>();

        var result = query.GetDefaultOrdering(metadata);

        result.Should().Be(nameof(TestQueryEntityWithoutAnyReadModelFields.AProperty2));
    }
    
    [Fact]
    public void WhenGetDefaultOrderingForEntityWithOverrideButReturnsNull_ThenReturnsLastSchemaColumn()
    {
        var query = Query.From<TestQueryEntityWithSortDefaultOverrideNull>()
            .WhereAll();
        var metadata = PersistedEntityMetadata.FromType<TestQueryEntityWithSortDefaultOverrideNull>();

        var result = query.GetDefaultOrdering(metadata);

        result.Should().Be(nameof(TestQueryEntityWithSortDefaultOverrideNull.AProperty));
    }

    [Fact]
    public void WhenGetDefaultOrderingForEntityWithOverride_ThenReturnsOverrideColumn()
    {
        var query = Query.From<TestQueryEntityWithSortDefaultOverride>()
            .WhereAll();
        var metadata = PersistedEntityMetadata.FromType<TestQueryEntityWithSortDefaultOverride>();

        var result = query.GetDefaultOrdering(metadata);

        result.Should().Be(nameof(TestQueryEntityWithSortDefaultOverride.DefaultSortBy));
    }

    [Fact]
    public void WhenGetDefaultOrderingForEntityWithDefaultOrderButNotInMetadata_ThenReturnsLastSchemaColumn()
    {
        var query = Query.From<TestQueryEntityWithReadModelFields>()
            .WhereAll()
            .OrderBy(x => x.LastPersistedAt);
        var metadata = PersistedEntityMetadata.FromType<TestQueryEntityWithoutAnyReadModelFields>();

        var result = query.GetDefaultOrdering(metadata);

        result.Should().Be(nameof(TestQueryEntityWithReadModelFields.AProperty));
    }

    [Fact]
    public void WhenGetDefaultOrderingForEntityWithDefaultOrder_ThenReturnsOrderingColumn()
    {
        var query = Query.From<TestQueryEntityWithReadModelFields>()
            .WhereAll()
            .OrderBy(x => x.AProperty);
        var metadata = PersistedEntityMetadata.FromType<TestQueryEntityWithReadModelFields>();

        var result = query.GetDefaultOrdering(metadata);

        result.Should().Be(nameof(TestQueryEntityWithReadModelFields.AProperty));
    }

    [Fact]
    public void WhenGetDefaultSkipAndZeroOffset_ThenReturnsZeroOffset()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll()
            .Skip(0);

        var result = query.GetDefaultSkip();

        result.Should().Be(0);
    }

    [Fact]
    public void WhenGetDefaultSkipAndNoOffset_ThenReturnsZero()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll();

        var result = query.GetDefaultSkip();

        result.Should().Be(0);
    }

    [Fact]
    public void WhenGetDefaultSkipAndSomeOffset_ThenReturnsOffset()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll()
            .Skip(1);

        var result = query.GetDefaultSkip();

        result.Should().Be(1);
    }

    [Fact]
    public void WhenGetDefaultTakeAndNoLimit_ThenReturnsMax()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll();

        var result = query.GetDefaultTake(99);

        result.Should().Be(99);
    }

    [Fact]
    public void WhenGetDefaultTakeAndSomeLimit_ThenReturnsLimit()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll()
            .Take(1);

        var result = query.GetDefaultTake(99);

        result.Should().Be(1);
    }
}

public class TestComplexType
{
    public string? AProperty { get; set; }
}

public class TestComplexTypeWithOverwrittenToString
{
    public string? AProperty { get; set; }

    public override string ToString()
    {
        return "overwritten";
    }
}

[UsedImplicitly]
public class TestQueryEntityWithoutAnyReadModelFields : IQueryableEntity
{
    public string? AProperty1 { get; set; }

    public string? AProperty2 { get; set; }
}

[UsedImplicitly]
public class TestQueryEntityWithIdOnly : IQueryableEntity
{
    public string? AProperty { get; set; }

    public string? Id { get; set; }
}

[UsedImplicitly]
public class TestQueryEntityWithReadModelFields : IQueryableEntity
{
    public string? AProperty { get; set; }

    public DateTime? LastPersistedAt { get; set; }

    public bool IsDeleted { get; set; }

    public string? Id { get; set; }
}

[UsedImplicitly]
public class TestQueryEntityWithoutId : IQueryableEntity
{
    public string? AProperty { get; set; }

    public DateTime? LastPersistedAt { get; set; }
}

[UsedImplicitly]
public class TestQueryEntityWithId : IQueryableEntity
{
    public string? AProperty { get; set; }

    public string? Id { get; set; }
}

[UsedImplicitly]
public class TestQueryEntityWithSortDefaultOverride : IQueryableEntity
{
    public string? AProperty { get; set; }

    public DateTime? DefaultSortBy { get; set; }

    // ReSharper disable once UnusedMember.Global
    public static string DefaultOrderingField()
    {
        return nameof(DefaultSortBy);
    }
}

[UsedImplicitly]
public class TestQueryEntityWithSortDefaultOverrideNull : IQueryableEntity
{
    public string? AProperty { get; set; }

    public DateTime? DefaultSortBy { get; set; }

    // ReSharper disable once UnusedMember.Global
    public static string DefaultOrderingField()
    {
        return null!;
    }
}