#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Converts the subscription to paid
/// </summary>
[Route("/subscriptions/{Id}/convert", OperationMethod.PutPatch, isTestingOnly: true)]
public class ConvertSubscriptionRequest : UnTenantedRequest<ConvertSubscriptionRequest, GetSubscriptionResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}
#endif