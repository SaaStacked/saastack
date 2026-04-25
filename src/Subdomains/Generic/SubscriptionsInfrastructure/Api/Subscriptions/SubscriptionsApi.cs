using Application.Resources.Shared;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class SubscriptionsApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly ISubscriptionsApplication _subscriptionsApplication;

    public SubscriptionsApi(ICallerContextFactory callerFactory, ISubscriptionsApplication subscriptionsApplication)
    {
        _callerFactory = callerFactory;
        _subscriptionsApplication = subscriptionsApplication;
    }

    public async Task<ApiResult<SubscriptionWithPlan, GetSubscriptionResponse>> CancelSubscription(
        CancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionsApplication.CancelSubscriptionAsync(_callerFactory.Create(),
            request.Id!, cancellationToken);

        return () =>
            subscription.HandleApplicationResult<SubscriptionWithPlan, GetSubscriptionResponse>(sub =>
                new GetSubscriptionResponse
                    { Subscription = sub });
    }

    public async Task<ApiPutPatchResult<SubscriptionWithPlan, GetSubscriptionResponse>> ChangePlan(
        ChangeSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionsApplication.ChangePlanAsync(_callerFactory.Create(),
            request.Id!, request.PlanId!, cancellationToken);

        return () =>
            subscription.HandleApplicationResult<SubscriptionWithPlan, GetSubscriptionResponse>(sub =>
                new GetSubscriptionResponse
                    { Subscription = sub });
    }

    public async Task<ApiResult<SubscriptionWithPlan, GetSubscriptionResponse>> ForceCancelSubscription(
        ForceCancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionsApplication.ForceCancelSubscriptionAsync(_callerFactory.Create(),
            request.Id!, cancellationToken);

        return () =>
            subscription.HandleApplicationResult<SubscriptionWithPlan, GetSubscriptionResponse>(sub =>
                new GetSubscriptionResponse
                    { Subscription = sub });
    }

    public async Task<ApiGetResult<SubscriptionWithPlan, GetSubscriptionResponse>> GetSubscription(
        GetSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionsApplication.GetSubscriptionByOwningEntityIdAsync(_callerFactory.Create(),
            request.Id!, cancellationToken);

        return () =>
            subscription.HandleApplicationResult<SubscriptionWithPlan, GetSubscriptionResponse>(sub =>
                new GetSubscriptionResponse
                    { Subscription = sub });
    }

    public async Task<ApiSearchResult<Invoice, SearchSubscriptionHistoryResponse>> SearchSubscriptionHistory(
        SearchSubscriptionHistoryRequest request, CancellationToken cancellationToken)
    {
        var history = await _subscriptionsApplication.SearchSubscriptionHistoryAsync(_callerFactory.Create(),
            request.Id!, request.FromUtc, request.ToUtc, request.ToSearchOptions(), request.ToGetOptions(),
            cancellationToken);

        return () =>
            history.HandleApplicationResult(c => new SearchSubscriptionHistoryResponse
                { Invoices = c.Results, Metadata = c.Metadata });
    }

    public async Task<ApiPutPatchResult<SubscriptionWithPlan, GetSubscriptionResponse>> TransferSubscription(
        TransferSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionsApplication.TransferSubscriptionAsync(_callerFactory.Create(),
            request.Id!, request.UserId!, cancellationToken);

        return () =>
            subscription.HandleApplicationResult<SubscriptionWithPlan, GetSubscriptionResponse>(sub =>
                new GetSubscriptionResponse { Subscription = sub });
    }

#if TESTINGONLY
    public async Task<ApiPutPatchResult<SubscriptionWithPlan, GetSubscriptionResponse>> ExpireTrial(
        ExpireSubscriptionTrialRequest request, CancellationToken cancellationToken)
    {
        var subscription =
            await _subscriptionsApplication.ExpireTrialAsync(_callerFactory.Create(), request.Id!, cancellationToken);

        return () =>
            subscription.HandleApplicationResult<SubscriptionWithPlan, GetSubscriptionResponse>(sub =>
                new GetSubscriptionResponse { Subscription = sub });
    }
#endif

#if TESTINGONLY
    public async Task<ApiPutPatchResult<SubscriptionWithPlan, GetSubscriptionResponse>> ConvertSubscription(
        ConvertSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription =
            await _subscriptionsApplication.ConvertSubscriptionAsync(_callerFactory.Create(), request.Id!,
                cancellationToken);

        return () =>
            subscription.HandleApplicationResult<SubscriptionWithPlan, GetSubscriptionResponse>(sub =>
                new GetSubscriptionResponse { Subscription = sub });
    }
#endif
}