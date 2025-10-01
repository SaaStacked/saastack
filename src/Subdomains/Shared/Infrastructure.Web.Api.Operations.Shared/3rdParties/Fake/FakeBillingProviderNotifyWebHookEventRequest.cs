#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Fake;

/// <summary>
///     Notifies when a fake billing provider backend event has occurred
/// </summary>
[Route("/webhooks/billingproviders/fake", OperationMethod.Post, isTestingOnly: true)]
public class
    FakeBillingProviderNotifyWebHookEventRequest : UnTenantedEmptyRequest<FakeBillingProviderNotifyWebHookEventRequest>
{
    public Dictionary<string, object>? Content { get; set; }

    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    public FakeBillingProviderEventType? EventType { get; set; }
}

public enum FakeBillingProviderEventType
{
    PaymentMethodCreated = 0
}
#endif