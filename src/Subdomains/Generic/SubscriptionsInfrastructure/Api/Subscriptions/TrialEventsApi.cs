using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class TrialEventsApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly ISubscriptionsApplication _subscriptionsApplication;

    public TrialEventsApi(ICallerContextFactory callerFactory, ISubscriptionsApplication subscriptionsApplication)
    {
        _callerFactory = callerFactory;
        _subscriptionsApplication = subscriptionsApplication;
    }

    public async Task<ApiPostResult<bool, DeliverMessageResponse>> DeliverSubscriptionTrialEvent(
        DeliverSubscriptionTrialEventRequest request, CancellationToken cancellationToken)
    {
        var resource =
            await _subscriptionsApplication.DeliverSubscriptionTrialEventAsync(_callerFactory.Create(),
                request.Message!, cancellationToken);

        return () => resource.HandleApplicationResult<bool, DeliverMessageResponse>(x =>
            new PostResult<DeliverMessageResponse>(new DeliverMessageResponse { IsSent = x }));
    }

#if TESTINGONLY
    public async Task<ApiEmptyResult> DrainAll(DrainAllSubscriptionTrialEventsRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _subscriptionsApplication.DrainAllSubscriptionTrialEventsAsync(_callerFactory.Create(),
                cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif
}