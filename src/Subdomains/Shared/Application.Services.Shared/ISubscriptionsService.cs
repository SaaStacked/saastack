using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface ISubscriptionsService
{
    Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionByIdAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<Error>> IncrementSubscriptionUsageAsync(ICallerContext caller, string owningEntityId, string eventName,
        CancellationToken cancellationToken);
}