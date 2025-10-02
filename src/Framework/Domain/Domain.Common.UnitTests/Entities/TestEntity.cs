﻿using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Interfaces.Entities;

namespace Domain.Common.UnitTests.Entities;

public sealed class TestEntity : EntityBase
{
    public static TestEntity Create(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler)
    {
        return new TestEntity(recorder, idFactory, rootEventHandler);
    }

    private TestEntity(IRecorder recorder, IIdentifierFactory idFactory, RootEventHandler rootEventHandler) : base(
        recorder, idFactory, rootEventHandler)
    {
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    public string? APropertyName { get; private set; }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        //Not used in testing
        return Result.Ok;
    }

    public void ChangeProperty(string value)
    {
        RaiseChangeEvent(new ChangeEvent { APropertyName = value });
    }

    public class ChangeEvent : IDomainEvent
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string? APropertyName { get; set; }

        public DateTime OccurredUtc { get; set; }

        public string RootId { get; set; } = "anentityid";
    }
}