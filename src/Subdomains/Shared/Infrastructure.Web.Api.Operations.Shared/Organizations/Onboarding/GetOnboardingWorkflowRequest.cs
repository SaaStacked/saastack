using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

/// <summary>
///     Fetches the onboarding workflow for an organization
/// </summary>
/// <response code="404">The onboarding process has not yet been initiated</response>
[Route("/organizations/{Id}/onboarding", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class GetOnboardingWorkflowRequest : UnTenantedRequest<GetOnboardingWorkflowRequest, GetOnboardingResponse>,
    IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}