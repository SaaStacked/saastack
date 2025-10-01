#if TESTINGONLY
using Application.Common;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Fake;
using Microsoft.AspNetCore.Http;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.Api._3rdParties;

public class FakeBillingProviderApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IRecorder _recorder;
    private readonly ISubscriptionsApplication _subscriptionsApplication;
    private readonly IWebhookNotificationAuditService _webHookNotificationAuditService;

    public FakeBillingProviderApi(IRecorder recorder, IHttpContextAccessor httpContextAccessor,
        ICallerContextFactory callerFactory, ISubscriptionsApplication subscriptionsApplication,
        IWebhookNotificationAuditService webHookNotificationAuditService)
    {
        _recorder = recorder;
        _callerFactory = callerFactory;
        _subscriptionsApplication = subscriptionsApplication;
        _webHookNotificationAuditService = webHookNotificationAuditService;
    }

    public async Task<ApiEmptyResult> NotifyWebhookEvent(FakeBillingProviderNotifyWebHookEventRequest request,
        CancellationToken cancellationToken)
    {
        var caller = _callerFactory.Create();
        var maintenance = Caller.CreateAsMaintenance(caller);

        var eventType = request.EventType.ToString()!;
        var created = await _webHookNotificationAuditService.CreateAuditAsync(caller,
            FakeBillingProviderConstants.AuditSourceName, request.EventId, eventType, request.ToJson(false),
            cancellationToken);
        if (created.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to audit Chargebee webhook event {Event} with {Code}: {Message}", eventType, created.Error.Code,
                created.Error.Message);
            return () => new Result<EmptyResponse, Error>(created.Error);
        }

        var audit = created.Value;
        if (request.EventType == FakeBillingProviderEventType.PaymentMethodCreated)
        {
            var customerId = request.Content!["customer_id"].ToString()!;
            var result =
                await NotifyBuyerPaymentMethodChangedAsync(maintenance, audit, customerId, cancellationToken);
            if (result.IsFailure)
            {
                return () => new Result<EmptyResponse, Error>(result.Error);
            }
        }

        return () => new Result<EmptyResponse, Error>(new EmptyResponse());
    }

    private async Task<Result<Error>> NotifyBuyerPaymentMethodChangedAsync(ICallerContext caller,
        WebhookNotificationAudit audit, string customerId, CancellationToken cancellationToken)
    {
        var retrievedState = await _subscriptionsApplication.GetProviderStateForBuyerAsync(caller,
            customerId, cancellationToken);
        if (retrievedState.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to find subscription for buyer reference {Buyer}, with {Code}: {Message}",
                customerId, retrievedState.Error.Code, retrievedState.Error.Message);
            return Result.Ok;
        }

        var updatedState = new SubscriptionMetadata(retrievedState.Value)
        {
            [FakeBillingProviderConstants.MetadataProperties.CustomerId] = customerId,
            [FakeBillingProviderConstants.MetadataProperties.PaymentMethodId] = "apaymentmethodid",
            [FakeBillingProviderConstants.MetadataProperties.PaymentMethodStatus] = "valid",
            [FakeBillingProviderConstants.MetadataProperties.PaymentMethodType] = "card"
        };

        var notified = await _subscriptionsApplication.NotifyBuyerPaymentMethodChangedAsync(caller,
            FakeBillingProviderConstants.ProviderName, updatedState, cancellationToken);
        if (notified.IsFailure)
        {
            _recorder.TraceError(caller.ToCall(),
                "Failed to notify buyer payment method change for buyer reference {Buyer}, with {Code}: {Message}",
                customerId, notified.Error.Code, notified.Error.Message);

            var updated =
                await _webHookNotificationAuditService.MarkAsFailedProcessingAsync(caller, audit.Id, cancellationToken);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            return notified.Error;
        }

        var saved = await _webHookNotificationAuditService.MarkAsProcessedAsync(caller, audit.Id, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result.Ok;
    }
}
#endif