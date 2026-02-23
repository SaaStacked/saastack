using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;

public class GetOnboardingResponse : IWebResponse
{
    public required OrganizationOnboardingWorkflow Workflow { get; set; }
}