using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using IdentityApplication.Persistence;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Tasks = Common.Extensions.Tasks;

namespace IdentityInfrastructure.Persistence;

public class APIKeysRepository : IAPIKeysRepository
{
    private readonly SnapshottingQueryStore<APIKeyAuth> _apiKeyQueries;
    private readonly IEventSourcingDddCommandStore<APIKeyRoot> _apiKeys;

    public APIKeysRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<APIKeyRoot> apiKeyStore, IDataStore store)
    {
        _apiKeyQueries = new SnapshottingQueryStore<APIKeyAuth>(recorder, domainFactory, store);
        _apiKeys = apiKeyStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _apiKeyQueries.DestroyAllAsync(cancellationToken),
            _apiKeys.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<Optional<APIKeyRoot>, Error>> FindByAPIKeyTokenAsync(string keyToken,
        CancellationToken cancellationToken)
    {
        var query = Query.From<APIKeyAuth>()
            .Where<string>(key => key.KeyToken, ConditionOperator.EqualTo, keyToken);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    public async Task<Result<APIKeyRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeys.LoadAsync(id, cancellationToken);
        if (apiKey.IsFailure)
        {
            return apiKey.Error;
        }

        return apiKey;
    }

    public async Task<Result<APIKeyRoot, Error>> SaveAsync(APIKeyRoot apiKey, CancellationToken cancellationToken)
    {
        var saved = await _apiKeys.SaveAsync(apiKey, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return apiKey;
    }

    public async Task<Result<QueryResults<APIKeyAuth>, Error>> SearchAllForUserAsync(Identifier userId,
        SearchOptions options, CancellationToken cancellationToken)
    {
        var query = Query.From<APIKeyAuth>()
            .Where<string>(key => key.UserId, ConditionOperator.EqualTo, userId)
            .WithSearchOptions(options);

        return await _apiKeyQueries.QueryAsync(query, false, cancellationToken);
    }

    public async Task<Result<QueryResults<APIKeyAuth>, Error>> SearchAllUnexpiredForUserAsync(Identifier userId,
        CancellationToken cancellationToken)
    {
        var query = Query.From<APIKeyAuth>()
            .Where<string>(key => key.UserId, ConditionOperator.EqualTo, userId)
            .AndWhere(subquery => subquery
                .Where<DateTime?>(key => key.ExpiresOn, ConditionOperator.EqualTo, null)
                .OrWhere<DateTime?>(key => key.ExpiresOn, ConditionOperator.GreaterThan, DateTime.UtcNow));

        return await _apiKeyQueries.QueryAsync(query, false, cancellationToken);
    }

    private async Task<Result<Optional<APIKeyRoot>, Error>> FindFirstByQueryAsync(QueryClause<APIKeyAuth> query,
        CancellationToken cancellationToken)
    {
        var queried = await _apiKeyQueries.QueryAsync(query, false, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<APIKeyRoot>.None;
        }

        var keys = await _apiKeys.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (keys.IsFailure)
        {
            return keys.Error;
        }

        return keys.Value.ToOptional();
    }
}