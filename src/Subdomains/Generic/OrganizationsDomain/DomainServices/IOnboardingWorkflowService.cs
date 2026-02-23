using Common;
using Domain.Common.ValueObjects;

namespace OrganizationsDomain.DomainServices;

/// <summary>
///     Defines a service for validating, manipulating, navigating an onboarding workflow
/// </summary>
public interface IOnboardingWorkflowService
{
    /// <summary>
    ///     Calculates the shortest path (by cumulative weight) from start to end step
    /// </summary>
    Journey CalculateShortestPath(IReadOnlyDictionary<string, StepSchema> steps, string startStepId,
        string endStepId);

    /// <summary>
    ///     Calculates the shortest path (by cumulative weight) from a given step to the end step
    /// </summary>
    Journey CalculateShortestPathToEnd(IReadOnlyDictionary<string, StepSchema> steps, string fromStepId,
        string endStepId);

    /// <summary>
    ///     Retrieves the workflow for the given organization
    /// </summary>
    Result<WorkflowSchema, Error> FindWorkflow(Identifier organizationId);

    /// <summary>
    ///     Validates that the workflow
    /// </summary>
    Result<Error> ValidateWorkflow(IReadOnlyDictionary<string, StepSchema> steps, string startStepId, string endStepId);
}