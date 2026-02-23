using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using OrganizationsApplication.Persistence.ReadModels;

namespace OrganizationsApplication.Persistence;

public interface IOnboardingCustomWorkflowRepository : IApplicationRepository
{
    Task<Result<OnboardingCustomWorkflow, Error>> LoadAsync(Identifier organizationId,
        CancellationToken cancellationToken);

    Task<Result<OnboardingCustomWorkflow, Error>> SaveAsync(OnboardingCustomWorkflow customWorkflow,
        CancellationToken cancellationToken);
}