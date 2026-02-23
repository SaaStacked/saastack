using Application.Resources.Shared;
using Common;
using OrganizationsDomain;

namespace OrganizationsApplication.ApplicationServices;

/// <summary>
///     Defines a service for managing custom onboarding workflows
/// </summary>
public interface ICustomOnboardingWorkflowService
{
    /// <summary>
    ///     Retrieves the workflow for the given organization
    /// </summary>
    Task<Result<WorkflowSchema, Error>> FindWorkflowAsync(string organizationId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Saves the workflow for the given organization
    /// </summary>
    Task<Result<OrganizationOnboardingWorkflowSchema, Error>> SaveWorkflowAsync(string organizationId,
        OrganizationOnboardingWorkflowSchema workflow, CancellationToken cancellationToken);
}