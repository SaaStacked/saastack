#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Drains all the pending subscription trial event messages
/// </summary>
[Route("/subscription-trial-events/drain", OperationMethod.Post, AccessType.HMAC, true)]
[Authorize(Roles.Platform_ServiceAccount)]
public class DrainAllSubscriptionTrialEventsRequest : UnTenantedEmptyRequest<DrainAllSubscriptionTrialEventsRequest>;
#endif