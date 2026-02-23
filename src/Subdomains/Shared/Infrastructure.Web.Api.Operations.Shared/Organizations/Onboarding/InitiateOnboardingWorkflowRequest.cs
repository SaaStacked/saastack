using System.ComponentModel.DataAnnotations;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

/// <summary>
///     Initiates the start of the specified onboarding workflow for an organization
/// </summary>
[Route("/organizations/{Id}/onboarding/initiate", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Owner, Features.Tenant_Basic)]
public class InitiateOnboardingWorkflowRequest :
    UnTenantedRequest<InitiateOnboardingWorkflowRequest, GetOnboardingResponse>, IUnTenantedOrganizationRequest
{
    [Required] public OrganizationOnboardingWorkflowSchema? Workflow { get; set; }

    public string? Id { get; set; }
}