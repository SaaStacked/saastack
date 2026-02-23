using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace OrganizationsApplication;

public interface IOnboardingApplication
{
    Task<Result<OrganizationOnboardingWorkflow, Error>> GetOnboardingAsync(ICallerContext caller, string organizationId,
        CancellationToken cancellationToken);

    Task<Result<OrganizationOnboardingWorkflow, Error>> InitiateOnboardingAsync(ICallerContext caller,
        string organizationId, OrganizationOnboardingWorkflowSchema workflowSchema,
        CancellationToken cancellationToken);

    Task<Result<OrganizationOnboardingWorkflow, Error>> MoveForwardAsync(ICallerContext caller, string organizationId,
        string? nextStepId, Dictionary<string, string>? stepValues, CancellationToken cancellationToken);

    Task<Result<OrganizationOnboardingWorkflow, Error>> MoveBackwardAsync(ICallerContext caller, string organizationId,
        CancellationToken cancellationToken);

    Task<Result<OrganizationOnboardingWorkflow, Error>> UpdateCurrentStepAsync(ICallerContext caller,
        string organizationId,
        Dictionary<string, string> stepValues, CancellationToken cancellationToken);

    Task<Result<OrganizationOnboardingWorkflow, Error>> CompleteOnboardingAsync(ICallerContext caller,
        string organizationId,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<OrganizationOnboardingWorkflow, Error>> ResetWorkflowAsync(ICallerContext caller, string organizationId,
        CancellationToken cancellationToken);
#endif
}