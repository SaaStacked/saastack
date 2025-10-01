#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.FakeProvider;

/// <summary>
///     Notifies when a fake billing provider backend event has occurred, and calls us to notify of it
/// </summary>
[Route("/webhooks/providers/billing/fake", OperationMethod.Post, isTestingOnly: true)]
public class
    NotifyFakeBillingProviderWebHookEventRequest : UnTenantedEmptyRequest<NotifyFakeBillingProviderWebHookEventRequest>
{
    public string? CustomerId { get; set; }

    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    public FakeBillingProviderEventType? EventType { get; set; }
}

public enum FakeBillingProviderEventType
{
    PaymentMethodCreated = 0
}
#endif