using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;

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