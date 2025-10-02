using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using CarsApplication.Persistence;
using CarsApplication.Persistence.ReadModels;
using CarsDomain;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared.Cars;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Unavailability = CarsApplication.Persistence.ReadModels.Unavailability;
using Tasks = Common.Extensions.Tasks;

namespace CarsInfrastructure.Persistence;

public class CarRepository : ICarRepository
{
    private readonly ISnapshottingQueryStore<Car> _carQueries;
    private readonly IEventSourcingDddCommandStore<CarRoot> _cars;
    private readonly ISnapshottingQueryStore<Unavailability> _unavailabilitiesQueries;

    public CarRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<CarRoot> carsStore, IDataStore store)
    {
        _carQueries = new SnapshottingQueryStore<Car>(recorder, domainFactory, store);
        _cars = carsStore;
        _unavailabilitiesQueries = new SnapshottingQueryStore<Unavailability>(recorder, domainFactory, store);
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _carQueries.DestroyAllAsync(cancellationToken),
            _cars.DestroyAllAsync(cancellationToken),
            _unavailabilitiesQueries.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<CarRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        var car = await _cars.LoadAsync(id, cancellationToken);
        if (car.IsFailure)
        {
            return car.Error;
        }

        return car.Value.OrganizationId != organizationId
            ? Error.EntityNotFound()
            : car;
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, bool reload, CancellationToken cancellationToken)
    {
        var saved = await _cars.SaveAsync(car, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync(car.OrganizationId, car.Id, cancellationToken)
            : car;
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, CancellationToken cancellationToken)
    {
        return await SaveAsync(car, false, cancellationToken);
    }

    public async Task<Result<QueryResults<Car>, Error>> SearchAllAvailableCarsAsync(Identifier organizationId,
        DateTime from, DateTime to, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var queriedUnavailabilities = await _unavailabilitiesQueries.QueryAsync(Query.From<Unavailability>()
                .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
                .AndWhere<DateTime>(u => u.From, ConditionOperator.LessThanEqualTo, from)
                .AndWhere<DateTime>(u => u.To, ConditionOperator.GreaterThanEqualTo, to),
            cancellationToken: cancellationToken);
        if (queriedUnavailabilities.IsFailure)
        {
            return queriedUnavailabilities.Error;
        }

        var unavailabilities = queriedUnavailabilities.Value.Results;
        var limit = searchOptions.Limit;
        var offset = searchOptions.Offset;
        searchOptions.ClearLimitAndOffset();

        var queriedCars = await _carQueries.QueryAsync(Query.From<Car>()
            .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .AndWhere<CarStatus>(c => c.Status, ConditionOperator.EqualTo, CarStatus.Registered)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (queriedCars.IsFailure)
        {
            return queriedCars.Error;
        }

        var allCars = queriedCars.Value.Results;
        var availableCars = allCars
            .Where(car => unavailabilities.All(unavailability => unavailability.CarId != car.Id))
            .Skip(offset)
            .Take(limit)
            .ToList();

        return new QueryResults<Car>(availableCars);
    }

    public async Task<Result<QueryResults<Car>, Error>> SearchAllCarsAsync(Identifier organizationId,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var queried = await _carQueries.QueryAsync(Query.From<Car>()
            .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        return queried.Value;
    }

    public async Task<Result<QueryResults<Unavailability>, Error>> SearchAllCarUnavailabilitiesAsync(
        Identifier organizationId, Identifier id, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var queried = await _unavailabilitiesQueries.QueryAsync(Query.From<Unavailability>()
            .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .AndWhere<string>(u => u.CarId, ConditionOperator.EqualTo, id)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        return queried.Value;
    }
}