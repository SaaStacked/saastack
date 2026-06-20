using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class QuotasApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly ISubscriptionsApplication _subscriptionsApplication;

    public QuotasApi(ICallerContextFactory callerFactory, ISubscriptionsApplication subscriptionsApplication)
    {
        _callerFactory = callerFactory;
        _subscriptionsApplication = subscriptionsApplication;
    }

#if TESTINGONLY
    public async Task<ApiEmptyResult> CheckQuota(CheckQuotaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _subscriptionsApplication.CheckQuotaAsync(_callerFactory.Create(),
            request.QuotaId!, request.Total, cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif
}