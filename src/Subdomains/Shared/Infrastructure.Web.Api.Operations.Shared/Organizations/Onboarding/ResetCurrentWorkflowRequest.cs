#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

/// <summary>
///     Resets a workflow for an organization.
/// </summary>
[Route("/organizations/{Id}/onboarding/reset", OperationMethod.PutPatch, AccessType.Token, true)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_PaidTrial)]
public class ResetCurrentWorkflowRequest : UnTenantedRequest<ResetCurrentWorkflowRequest, GetOnboardingResponse>,
    IUnTenantedOrganizationRequest
{
    [Required] public string? Id { get; set; }
}
#endif