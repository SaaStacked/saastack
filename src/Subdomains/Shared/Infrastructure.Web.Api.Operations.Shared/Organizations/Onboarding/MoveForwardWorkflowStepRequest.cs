using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

/// <summary>
///     Moves forward in the onboarding workflow, either to specifed step, or to the next step.
///     If the current step is a branching step, then the next step will evaluate branching conditions to determine the
///     next step. Include values to update the current step, before moving forward.
/// </summary>
[Route("/organizations/{Id}/onboarding/next", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class MoveForwardWorkflowStepRequest : UnTenantedRequest<MoveForwardWorkflowStepRequest, GetOnboardingResponse>,
    IUnTenantedOrganizationRequest
{
    public string? NextStepId { get; set; }

    public Dictionary<string, string>? Values { get; set; }

    public string? Id { get; set; }
}