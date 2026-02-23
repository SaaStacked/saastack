using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using OrganizationsDomain;

namespace OrganizationsApplication.Persistence;

public interface IOnboardingRepository : IApplicationRepository
{
    Task<Result<Optional<OrganizationOnboardingRoot>, Error>> FindByOrganizationIdAsync(Identifier organizationId,
        CancellationToken cancellationToken);

    Task<Result<OrganizationOnboardingRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<OrganizationOnboardingRoot, Error>> SaveAsync(OrganizationOnboardingRoot onboarding,
        CancellationToken cancellationToken);

    Task<Result<OrganizationOnboardingRoot, Error>> SaveAsync(OrganizationOnboardingRoot onboarding, bool reload,
        CancellationToken cancellationToken);
}