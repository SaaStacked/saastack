using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Infrastructure.Common.UnitTests;

public class TestAggregateRoot : AggregateRootBase
{
    public TestAggregateRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    public TestAggregateRoot(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    public TestAggregateRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) :
        base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<TestAggregateRoot> Rehydrate()
    {
        return (identifier, container, rehydratingProperties) =>
            new TestAggregateRoot(identifier, container, rehydratingProperties);
    }
}

[UsedImplicitly]
public class TestAggregateRoot2 : AggregateRootBase
{
    public TestAggregateRoot2(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    public TestAggregateRoot2(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    public TestAggregateRoot2(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) :
        base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<TestAggregateRoot2> Rehydrate()
    {
        return (identifier, container, rehydratingProperties) =>
            new TestAggregateRoot2(identifier, container, rehydratingProperties);
    }
}

[UsedImplicitly]
public class TestAggregateRoot3 : AggregateRootBase
{
    public TestAggregateRoot3(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    public TestAggregateRoot3(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
    }

    public TestAggregateRoot3(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) :
        base(recorder, idFactory, identifier)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public static AggregateRootFactory<TestAggregateRoot3> Rehydrate()
    {
        return (identifier, container, rehydratingProperties) =>
            new TestAggregateRoot3(identifier, container, rehydratingProperties);
    }
}