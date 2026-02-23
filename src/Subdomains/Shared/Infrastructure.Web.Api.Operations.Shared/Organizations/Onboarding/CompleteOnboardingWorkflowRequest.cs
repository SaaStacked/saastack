using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

/// <summary>
///     Completes the onboarding workflow for an organization
/// </summary>
[Route("/organizations/{Id}/onboarding/complete", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class CompleteOnboardingWorkflowRequest :
    UnTenantedRequest<CompleteOnboardingWorkflowRequest, GetOnboardingResponse>, IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}