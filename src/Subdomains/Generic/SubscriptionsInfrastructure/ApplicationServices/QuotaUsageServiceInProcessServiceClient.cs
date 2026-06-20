using Application.Interfaces;
using Application.Services.Shared;
using Common;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.ApplicationServices;

public class QuotaUsageServiceInProcessServiceClient : IQuotaUsageService
{
    private readonly ISubscriptionsApplication _subscriptionsApplication;

    public QuotaUsageServiceInProcessServiceClient(ISubscriptionsApplication subscriptionsApplication)
    {
        _subscriptionsApplication = subscriptionsApplication;
    }

    public async Task<Result<Error>> TryCheckQuotaUsageAsync(ICallerContext caller, string quotaId,
        long proposedTotal, CancellationToken cancellationToken)
    {
        return await _subscriptionsApplication.TryCheckQuotaUsageAsync(caller, quotaId, proposedTotal,
            cancellationToken);
    }
}