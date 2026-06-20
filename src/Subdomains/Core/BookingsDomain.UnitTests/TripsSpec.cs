using Common;
using Domain.Common.Identity;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace BookingsDomain.UnitTests;

[Trait("Category", "Unit")]
public class TripsSpec
{
    private readonly Trips _trips = new();

    [Fact]
    public void WhenAdd_ThenAddsTrip()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new FixedIdentifierFactory("anid");
        var trip = Trip.Create(recorder.Object, idFactory, _ => Result.Ok).Value;

        _trips.Add(trip);

        _trips.Count().Should().Be(1);
        _trips.First().Should().Be(trip);
    }

    [Fact]
    public void WhenLatestAndNone_ThenReturnsNull()
    {
        var result = _trips.Latest();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenLatestAndSome_ThenReturnsLast()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new FixedIdentifierFactory("anid");
        var trip = Trip.Create(recorder.Object, idFactory, _ => Result.Ok).Value;
        _trips.Add(trip);

        var result = _trips.Latest();

        result.Should().Be(trip);
    }

    [Fact]
    public void WhenFindByIdAndUnknown_ThenReturnsNone()
    {
        var result = _trips.FindById("atripid");

        result.Should().BeNone();
    }

    [Fact]
    public void WhenFindById_ThenReturnsTrip()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new FixedIdentifierFactory("anid");
        var trip = Trip.Create(recorder.Object, idFactory, _ => Result.Ok).Value;
        _trips.Add(trip);

        var result = _trips.FindById("anid");

        result.Should().Be(trip);
    }
}