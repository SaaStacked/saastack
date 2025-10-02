﻿using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.UnitTests.Entities;

public class TestAggregateRoot : AggregateRootBase
{
    public static TestAggregateRoot Create(IRecorder recorder, IIdentifierFactory idFactory)
    {
        var aggregate = new TestAggregateRoot(recorder, idFactory);
        aggregate.RaiseCreateEvent(new CreateEvent { RootId = aggregate.Id });
        return aggregate;
    }

    private TestAggregateRoot(IRecorder recorder, IIdentifierFactory idFactory)
        : base(recorder, idFactory)
    {
    }

    private TestAggregateRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier)
        : base(recorder, idFactory, identifier)
    {
    }

    public static AggregateRootFactory<TestAggregateRoot> Rehydrate()
    {
        return (identifier, container, _) => new TestAggregateRoot(
            container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }

    public void ChangeProperty(string value)
    {
        RaiseChangeEvent(new ChangeEvent { APropertyName = value });
    }

    public class CreateEvent : DomainEvent
    {
        public CreateEvent() : base("anid")
        {
        }
    }

    public class ChangeEvent : DomainEvent
    {
        public ChangeEvent() : base("anid")
        {
        }

        public required string APropertyName { get; set; }
    }
}

public class TestAggregateRoot2 : AggregateRootBase
{
    public static TestAggregateRoot2 Create(IRecorder recorder, IIdentifierFactory idFactory)
    {
        var aggregate = new TestAggregateRoot2(recorder, idFactory);
        aggregate.RaiseCreateEvent(new TestAggregateRoot.CreateEvent { RootId = aggregate.Id });
        return aggregate;
    }

    private TestAggregateRoot2(IRecorder recorder, IIdentifierFactory idFactory)
        : base(recorder, idFactory)
    {
    }

    private TestAggregateRoot2(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier)
        : base(recorder, idFactory, identifier)
    {
    }

    public static AggregateRootFactory<TestAggregateRoot2> Rehydrate()
    {
        return (identifier, container, _) => new TestAggregateRoot2(
            container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        return Result.Ok;
    }
}