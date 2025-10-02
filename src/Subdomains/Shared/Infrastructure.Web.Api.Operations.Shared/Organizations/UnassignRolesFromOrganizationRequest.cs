using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Unassigns the list of roles from a member of an organization
/// </summary>
[Route("/organizations/{Id}/roles/unassign", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Interfaces.Roles.Tenant_Owner, Features.Tenant_PaidTrial)]
public class UnassignRolesFromOrganizationRequest :
    UnTenantedRequest<UnassignRolesFromOrganizationRequest, GetOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    public List<string> Roles { get; set; } = [];

    [Required] public string? UserId { get; set; }

    public string? Id { get; set; }
}