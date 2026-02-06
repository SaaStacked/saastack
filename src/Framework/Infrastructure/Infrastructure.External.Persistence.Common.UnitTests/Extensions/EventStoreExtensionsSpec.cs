using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Infrastructure.External.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using Moq;
using QueryAny;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.External.Persistence.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class EventStoreExtensionsSpec
{
    private readonly Mock<IEventStore> _eventStore = new();

    [Fact]
    public void WhenVerifyContiguousCheckAndNothingStoredAndFirstVersionIsNotFirst_ThenReturnsError()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", Optional<int>.None, 10);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStore_VerifyContiguousCheck_StreamReset.Format("IEventStoreProxy", "astreamname"));
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndNothingStoredAndFirstVersionIsFirst_ThenPasses()
    {
        var result =
            _eventStore.Object.VerifyContiguousCheck("astreamname", Optional<int>.None, EventStream.FirstVersion);

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndFirstVersionIsSameAsStored_ThenReturnsError()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", 2, 2);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStore_VerifyContiguousCheck_VersionCollision.Format("IEventStoreProxy", "astreamname",
                2, 2));
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndFirstVersionIsBeforeStored_ThenReturnsError()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", 2, 1);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStore_VerifyContiguousCheck_VersionCollision.Format("IEventStoreProxy", "astreamname",
                1, 2));
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndFirstVersionIsAfterStoredButNotContiguous_ThenReturnsError()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", 1, 3);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStore_VerifyContiguousCheck_MissingVersions.Format("IEventStoreProxy", "astreamname",
                2, 3));
    }

    [Fact]
    public void WhenVerifyContiguousCheckAndFirstVersionIsNextAfterStored_ThenPasses()
    {
        var result = _eventStore.Object.VerifyContiguousCheck("astreamname", 1, 2);

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenGetEntityNameAndHasEntityAttribute_ThenReturnsAttributeName()
    {
        var result = EventStoreExtensions.GetAggregateName<TestAggregateRootWithAttribute>();

        result.Should().Be("acontainername");
    }

    [Fact]
    public void WhenGetEntityNameAndAggregateRootNameEndsWithAggregate_ThenReturnsTruncatedName()
    {
        var result = EventStoreExtensions.GetAggregateName<TestAggregateWithSuffixAggregate>();

        result.Should().Be("TestAggregateWithSuffix");
    }

    [Fact]
    public void WhenGetEntityNameAndAggregateRootNameEndsWithRoot_ThenReturnsTruncatedName()
    {
        var result = EventStoreExtensions.GetAggregateName<TestAggregateWithSuffixRoot>();

        result.Should().Be("TestAggregateWithSuffix");
    }

    [Fact]
    public void WhenGetEntityNameAndAggregateRootNameEndsWithEntity_ThenReturnsTruncatedName()
    {
        var result = EventStoreExtensions.GetAggregateName<TestAggregateWithSuffixEntity>();

        result.Should().Be("TestAggregateWithSuffix");
    }
}

[EntityName("acontainername")]
[UsedImplicitly]
public class TestAggregateRootWithAttribute
{
}

[UsedImplicitly]
public class TestAggregateWithSuffixAggregate
{
}

[UsedImplicitly]
public class TestAggregateWithSuffixRoot
{
}

[UsedImplicitly]
public class TestAggregateWithSuffixEntity
{
}