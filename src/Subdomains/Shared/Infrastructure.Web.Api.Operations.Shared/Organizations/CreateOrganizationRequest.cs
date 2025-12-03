using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Creates a new organization to share with other users
/// </summary>
/// <response code="405">
///     This user's email address is not allowed to create a shared organization, it may be a personal email
///     address. It must be a company email address
/// </response>
/// <response code="409">Another user has already claimed an organization with the same company email address as yours</response>
[Route("/organizations", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class CreateOrganizationRequest : UnTenantedRequest<CreateOrganizationRequest, GetOrganizationResponse>
{
    [Required] public string? Name { get; set; }
}