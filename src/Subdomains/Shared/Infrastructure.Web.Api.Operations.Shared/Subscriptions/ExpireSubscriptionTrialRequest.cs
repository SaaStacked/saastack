#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Expires the subscriptions trial
/// </summary>
[Route("/subscriptions/{Id}/expire-trial", OperationMethod.PutPatch, isTestingOnly: true)]
public class ExpireSubscriptionTrialRequest :
    UnTenantedRequest<ExpireSubscriptionTrialRequest, GetSubscriptionResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}
#endif