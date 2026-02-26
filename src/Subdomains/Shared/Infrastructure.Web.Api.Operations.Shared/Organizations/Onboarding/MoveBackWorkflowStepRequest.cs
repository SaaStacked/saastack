using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

/// <summary>
///     Moves backward to the previous step in the onboarding workflow
/// </summary>
/// <response code="405">The current step is the first step. Or, the workflow has been completed</response>
[Route("/organizations/{Id}/onboarding/back", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class MoveBackWorkflowStepRequest : UnTenantedRequest<MoveBackWorkflowStepRequest, GetOnboardingResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}