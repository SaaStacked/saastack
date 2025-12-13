using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Removes a previously invited member from the organization
/// </summary>
/// <response code="400">The invited cannot be removed from a personal organization</response>
/// <response code="403">The inviter is not an owner of the organization, or the invited is a required member of the organization</response>
[Route("/organizations/{Id}/members/{UserId}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Platform_PaidTrial)]
public class UnInviteMemberFromOrganizationRequest : UnTenantedRequest<UnInviteMemberFromOrganizationRequest,
        UnInviteMemberFromOrganizationResponse>,
    IUnTenantedOrganizationRequest
{
    [Required] public string? UserId { get; set; }

    public string? Id { get; set; }
}