using Common;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;

namespace Infrastructure.Common.UnitTests;

public class TestPersistableEntity : IDehydratableEntity
{
    private TestPersistableEntity(HydrationProperties properties)
    {
        var id = properties.GetValueOrDefault<Identifier>(nameof(Id));
        Id = id.ValueOrDefault!;
        LastPersistedAt = properties.GetValueOrDefault<DateTime>(nameof(LastPersistedAt));
        APropertyValue = properties.GetValueOrDefault<string>(nameof(APropertyValue));
    }

    public string APropertyValue { get; private set; }

    public static EntityFactory<TestPersistableEntity> Rehydrate()
    {
        return (_, _, properties) => new TestPersistableEntity(properties);
    }

    public HydrationProperties Dehydrate()
    {
        return new HydrationProperties
        {
            { nameof(Id), Id },
            { nameof(LastPersistedAt), LastPersistedAt },
            { nameof(APropertyValue), APropertyValue }
        };
    }

    public ISingleValueObject<string> Id { get; private set; }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public Optional<bool> IsDeleted { get; }

    public Optional<DateTime> LastPersistedAt { get; }
}