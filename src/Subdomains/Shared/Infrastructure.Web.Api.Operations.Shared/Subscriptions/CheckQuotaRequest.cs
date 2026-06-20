using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Subscriptions;

/// <summary>
///     Checks the limits of the specified quota
/// </summary>
[Route("/testingonly/quotas", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class CheckQuotaRequest : UnTenantedRequest<CheckQuotaRequest, CheckQuotaResponse>,
    IUnTenantedOrganizationRequest
{
    public string? QuotaId { get; set; }

    public long Total { get; set; }

    public string? Id { get; set; }
}