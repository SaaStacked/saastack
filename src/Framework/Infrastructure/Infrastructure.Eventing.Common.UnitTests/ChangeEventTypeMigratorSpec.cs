using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Extensions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Eventing.Common.UnitTests;

[Trait("Category", "Unit")]
public class ChangeEventTypeMigratorSpec
{
    [Fact]
    public void WhenRehydrateAndEventTypeExistsButNoMapping_ThenReturnsExistingInstance()
    {
        var eventJson = new TestPreviousChangeEvent().ToEventJson();
        var originalEventTypeName = typeof(TestPreviousChangeEvent).AssemblyQualifiedName!;
        var migrator = new ChangeEventTypeMigrator(new Dictionary<string, Type>());

        var result = migrator.Rehydrate("aneventid", eventJson, originalEventTypeName);

        result.Value.Should().BeOfType<TestPreviousChangeEvent>();
        result.Value.As<TestPreviousChangeEvent>().RootId.Should().Be("arootid");
    }

    [Fact]
    public void WhenRehydrateAndEventTypeNotExistsAndNoMapping_ThenReturnsError()
    {
        var eventJson = new TestPreviousChangeEvent().ToEventJson();
        var migrator = new ChangeEventTypeMigrator(new Dictionary<string, Type>());

        var result = migrator.Rehydrate("aneventid", eventJson, "anunknowntype");

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.ChangeEventMigrator_UnknownType.Format("aneventid", "anunknowntype"));
    }

    [Fact]
    public void WhenRehydrateAndMappingExistsButCannotBeDeserialized_ThenReturnsError()
    {
        var newEventType = typeof(TestUnDeserializableChangeEvent);
        var eventJson = "{}";
        var migrator = new ChangeEventTypeMigrator(new Dictionary<string, Type>
        {
            { "apreviouseventtype", newEventType }
        });

        var result = migrator.Rehydrate("aneventid", eventJson, "apreviouseventtype");

        result.Should().BeError(ErrorCode.Unexpected,
            Resources.ChangeEventMigrator_FailedToDeserialize.Format("aneventid", "apreviouseventtype",
                $"JSON deserialization for type '{typeof(TestUnDeserializableChangeEvent).FullName}' was missing required properties, including the following: {nameof(TestUnDeserializableChangeEvent.ARequiredProperty)}"));
    }
    
    [Fact]
    public void WhenRehydrateAndMappingExists_ThenReturnsNewInstance()
    {
        var newEventType = typeof(TestRenamedChangeEvent);
        var eventJson = new TestPreviousChangeEvent().ToEventJson();
        var migrator = new ChangeEventTypeMigrator(new Dictionary<string, Type>
        {
            { "apreviouseventtype", newEventType }
        });

        var result = migrator.Rehydrate("aneventid", eventJson, "apreviouseventtype");

        result.Should().BeSuccess();
        result.Value.Should().BeOfType<TestRenamedChangeEvent>();
        result.Value.As<TestRenamedChangeEvent>().RootId.Should().Be("arootid");
    }
}

public class TestPreviousChangeEvent : DomainEvent
{
    public TestPreviousChangeEvent() : base("arootid")
    {
    }
}

public class TestUnDeserializableChangeEvent : DomainEvent
{
    public TestUnDeserializableChangeEvent() : base("arootid")
    {
    }

    public required string ARequiredProperty { get; set; }
}

public class TestRenamedChangeEvent : DomainEvent
{
    public TestRenamedChangeEvent() : base("arootid")
    {
    }
}