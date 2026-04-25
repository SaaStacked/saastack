using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Delivers a subscription trial event message
/// </summary>
[Route("/subscription-trial-events/deliver", OperationMethod.Post, AccessType.HMAC)]
[Authorize(Roles.Platform_ServiceAccount)]
public class
    DeliverSubscriptionTrialEventRequest : UnTenantedRequest<DeliverSubscriptionTrialEventRequest,
    DeliverMessageResponse>
{
    [Required] public string? Message { get; set; }
}