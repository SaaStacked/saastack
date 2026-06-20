using System.Collections;
using Common;

namespace BookingsDomain;

public class Trips : IReadOnlyList<Trip>
{
    private readonly List<Trip> _trips = new();

    public Result<Error> EnsureInvariants()
    {
        foreach (var trip in _trips)
        {
            var ensured = trip.EnsureInvariants();
            if (ensured.IsFailure)
            {
                return ensured.Error;
            }
        }

        return Result.Ok;
    }

    public int Count => _trips.Count;

    public IEnumerator<Trip> GetEnumerator()
    {
        return _trips.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Trip this[int index] => _trips[index];

    public void Add(Trip trip)
    {
        _trips.Add(trip);
    }

    public Trip? Latest()
    {
        return _trips.LastOrDefault();
    }

    public Optional<Trip> FindById(string tripId)
    {
        return _trips
            .SingleOrDefault(ms => ms.Id == tripId);
    }
}