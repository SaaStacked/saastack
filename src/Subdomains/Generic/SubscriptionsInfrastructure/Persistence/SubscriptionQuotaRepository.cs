using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using SubscriptionsApplication.Persistence;
using SubscriptionsApplication.Persistence.ReadModels;
using SubscriptionsDomain;

namespace SubscriptionsInfrastructure.Persistence;

public class SubscriptionQuotaRepository : ISubscriptionQuotaRepository
{
    private readonly ISnapshottingQueryStore<SubscriptionQuotaUsage> _usageQueries;
    private readonly ISnapshottingDddCommandStore<SubscriptionQuotaUsageRoot> _usages;

    public SubscriptionQuotaRepository(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _usages = new SnapshottingDddCommandStore<SubscriptionQuotaUsageRoot>(recorder, domainFactory, store);
        _usageQueries = new SnapshottingQueryStore<SubscriptionQuotaUsage>(recorder, domainFactory, store);
    }

    public async Task<Result<Error>> DeleteUsageAsync(Identifier owningEntityId, Identifier id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _usages.GetAsync(id, true, false, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (retrieved.Value.Value.OwningEntityId != owningEntityId)
        {
            return Error.EntityNotFound();
        }

        var deleted = await _usages.DeleteAsync(id, true, cancellationToken);
        return deleted.IsFailure
            ? deleted.Error
            : Result.Ok;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await _usages.DestroyAllAsync(cancellationToken);
    }
#endif

    public async Task<Result<SubscriptionQuotaUsageRoot, Error>> LoadAsync(Identifier owningEntityId, Identifier id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _usages.GetAsync(id, true, false, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var usage = retrieved.Value.Value;
        if (usage.OwningEntityId != owningEntityId)
        {
            return Error.EntityNotFound();
        }

        return usage;
    }

    public async Task<Result<SubscriptionQuotaUsageRoot, Error>> SaveAsync(SubscriptionQuotaUsageRoot usage,
        bool reload,
        CancellationToken cancellationToken)
    {
        var upserted = await _usages.UpsertAsync(usage, false, cancellationToken);
        if (upserted.IsFailure)
        {
            return upserted.Error;
        }

        return upserted.Value;
    }

    public async Task<Result<SubscriptionQuotaUsageRoot, Error>> SaveAsync(SubscriptionQuotaUsageRoot usage,
        CancellationToken cancellationToken)
    {
        return await SaveAsync(usage, false, cancellationToken);
    }

    public async Task<Result<QueryResults<SubscriptionQuotaUsage>, Error>> SearchAllByOwningEntityIdAsync(
        string providerName, Identifier owningEntityId, SearchOptions searchOptions,
        CancellationToken cancellationToken)
    {
        var queried = await _usageQueries.QueryAsync(Query.From<SubscriptionQuotaUsage>()
            .Where<string>(u => u.ProviderName, ConditionOperator.EqualTo, providerName)
            .AndWhere<string>(u => u.OwningEntityId, ConditionOperator.EqualTo, owningEntityId)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        return queried.Value;
    }
}