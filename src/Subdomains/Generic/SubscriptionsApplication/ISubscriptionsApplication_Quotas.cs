using Application.Interfaces;
using Common;

namespace SubscriptionsApplication;

public partial interface ISubscriptionsApplication
{
#if TESTINGONLY
    Task<Result<Error>> CheckQuotaAsync(ICallerContext caller, string quotaId, long total,
        CancellationToken cancellationToken);
#endif

    Task<Result<Error>> TryCheckQuotaUsageAsync(ICallerContext caller, string quotaId, long proposedTotal,
        CancellationToken cancellationToken);
}