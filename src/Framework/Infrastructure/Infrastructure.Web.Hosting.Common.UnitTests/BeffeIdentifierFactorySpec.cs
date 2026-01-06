using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests;

[Trait("Category", "Unit")]
public class BeffeIdentifierFactorySpec
{
    private readonly BeffeIdentifierFactory _factory;

    public BeffeIdentifierFactorySpec()
    {
        _factory = new BeffeIdentifierFactory();
    }

    [Fact]
    public void WhenCreate_ThenThrows()
    {
        _factory.Invoking(f => f.Create(new TestEntity()))
            .Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void WhenIsValidAndEmpty_ThenReturnsFalse()
    {
        var result = _factory.IsValid(Identifier.Empty());

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidAndInvalidForm_ThenReturnsFalse()
    {
        var result = _factory.IsValid("anid".ToId());

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidAndCorrectForm_ThenReturnsTrue()
    {
        var result = _factory.IsValid("anentity_1234567890123456789012".ToId());

        result.Should().BeTrue();
    }
}

public class TestEntity : IIdentifiableEntity
{
    public ISingleValueObject<string> Id { get; } = null!;
}