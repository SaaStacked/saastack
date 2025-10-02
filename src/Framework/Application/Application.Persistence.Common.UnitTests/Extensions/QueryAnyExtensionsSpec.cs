﻿using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using FluentAssertions;
using QueryAny;
using Xunit;

namespace Application.Persistence.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class QueryAnyExtensionsSpec
{
    [Fact]
    public void WhenWithSearchOptionsWithDefaultOptions_ThenReturnsQuery()
    {
        var query = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions());

        query.ResultOptions.Offset.Should().Be(ResultOptions.DefaultOffset);
        query.ResultOptions.Limit.Should().Be(SearchOptions.DefaultLimit);
        query.ResultOptions.OrderBy.By.Should().BeNull();
        query.ResultOptions.OrderBy.Direction.Should().Be(OrderDirection.Ascending);
        query.PrimaryEntity.Selects.Count.Should().Be(0);
    }

    [Fact]
    public void WhenWithSearchOptionsWithOffset_ThenReturnsQuery()
    {
        var query = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions { Offset = 9 });

        query.ResultOptions.Offset.Should().Be(9);
    }

    [Fact]
    public void WhenWithSearchOptionsWithOffsetAtZero_ThenReturnsQuery()
    {
        var query = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions { Offset = 0 });

        query.ResultOptions.Offset.Should().Be(0);
    }

    [Fact]
    public void WhenWithSearchOptionsWithLimit_ThenReturnsQuery()
    {
        var query = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions { Limit = 9 });

        query.ResultOptions.Limit.Should().Be(9);
    }

    [Fact]
    public void WhenWithSearchOptionsWithUnknownSortProperty_ThenReturnsQuery()
    {
        var query = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions
                { Sort = new Sorting("afieldname", SortDirection.Descending) });

        query.ResultOptions.OrderBy.By.Should().BeNull();
        query.ResultOptions.OrderBy.Direction.Should().Be(ResultOptions.DefaultOrderDirection);
    }

    [Fact]
    public void WhenWithSearchOptionsWithSortDescending_ThenReturnsQuery()
    {
        var query = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions
            {
                Sort = new Sorting(nameof(TestEntity.APropertyName), SortDirection.Descending)
            });

        query.ResultOptions.OrderBy.By.Should().Be(nameof(TestEntity.APropertyName));
        query.ResultOptions.OrderBy.Direction.Should().Be(OrderDirection.Descending);
    }

    [Fact]
    public void WhenWithSearchOptionsWithUnknownFilterFields_ThenReturnsQuery()
    {
        var query = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions
            {
                Filter = new Filtering("afieldname")
            });

        query.PrimaryEntity.Selects.Count.Should().Be(0);
    }

    [Fact]
    public void WhenWithSearchOptionsWithLowerCaseFilterFields_ThenReturnsQuery()
    {
        var query = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions
            {
                Filter = new Filtering(nameof(TestEntity.APropertyName).ToLower())
            });

        query.PrimaryEntity.Selects.Count.Should().Be(1);
        query.PrimaryEntity.Selects[0].EntityName.Should().Be("Test");
        query.PrimaryEntity.Selects[0].FieldName.Should().Be(nameof(TestEntity.APropertyName));
        query.PrimaryEntity.Selects[0].JoinedEntityName.Should().BeNull();
        query.PrimaryEntity.Selects[0].JoinedFieldName.Should().BeNull();
    }

    [Fact]
    public void WhenWithSearchOptionsWithFilterFields_ThenReturnsQuery()
    {
        var query = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions
            {
                Filter = new Filtering(nameof(TestEntity.APropertyName))
            });

        query.PrimaryEntity.Selects.Count.Should().Be(1);
        query.PrimaryEntity.Selects[0].EntityName.Should().Be("Test");
        query.PrimaryEntity.Selects[0].FieldName.Should().Be(nameof(TestEntity.APropertyName));
        query.PrimaryEntity.Selects[0].JoinedEntityName.Should().BeNull();
        query.PrimaryEntity.Selects[0].JoinedFieldName.Should().BeNull();
    }

    [Fact]
    public void WhenIsPaginatingAndNoSearchOptions_ThenReturnsFalse()
    {
        var result = Query.Empty<TestEntity>()
            .IsPaginating(50);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsPaginatingAndResultsAreLessThanLimit_ThenReturnsFalse()
    {
        var result = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions { Limit = 51 })
            .IsPaginating(50);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsPaginatingAndResultsAreSameAsLimit_ThenReturnsTrue()
    {
        var result = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions { Limit = 50 })
            .IsPaginating(50);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsPaginatingAndResultsAreGreaterThanLimit_ThenReturnsTrue()
    {
        var result = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions { Limit = 50 })
            .IsPaginating(51);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsPaginatingAndAndZeroOffset_ThenReturnsTrue()
    {
        var result = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions { Offset = 0 })
            .IsPaginating(50);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsPaginatingAndAndCustomOffset_ThenReturnsTrue()
    {
        var result = Query.Empty<TestEntity>()
            .WithSearchOptions(new SearchOptions { Offset = 1 })
            .IsPaginating(50);

        result.Should().BeTrue();
    }
}