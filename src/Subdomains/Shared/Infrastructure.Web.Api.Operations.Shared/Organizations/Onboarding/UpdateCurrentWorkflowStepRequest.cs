using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

/// <summary>
///     Updates the current step data without advancing in the onboarding workflow
/// </summary>
[Route("/organizations/{Id}/onboarding", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class UpdateCurrentWorkflowStepRequest :
    UnTenantedRequest<UpdateCurrentWorkflowStepRequest, GetOnboardingResponse>, IUnTenantedOrganizationRequest
{
    [Required] public Dictionary<string, string>? Values { get; set; }

    public string? Id { get; set; }
}