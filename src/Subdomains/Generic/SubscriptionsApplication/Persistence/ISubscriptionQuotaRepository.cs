using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using SubscriptionsApplication.Persistence.ReadModels;
using SubscriptionsDomain;

namespace SubscriptionsApplication.Persistence;

public interface ISubscriptionQuotaRepository : IApplicationRepository
{
    Task<Result<Error>> DeleteUsageAsync(Identifier owningEntityId, Identifier id, CancellationToken cancellationToken);

    Task<Result<SubscriptionQuotaUsageRoot, Error>> LoadAsync(Identifier owningEntityId, Identifier id,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionQuotaUsageRoot, Error>> SaveAsync(SubscriptionQuotaUsageRoot usage, bool reload,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionQuotaUsageRoot, Error>> SaveAsync(SubscriptionQuotaUsageRoot usage,
        CancellationToken cancellationToken);

    Task<Result<QueryResults<SubscriptionQuotaUsage>, Error>> SearchAllByOwningEntityIdAsync(string providerName,
        Identifier owningEntityId, SearchOptions searchOptions, CancellationToken cancellationToken);
}