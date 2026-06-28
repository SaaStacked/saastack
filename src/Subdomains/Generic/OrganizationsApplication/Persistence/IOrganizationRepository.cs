using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using OrganizationsApplication.Persistence.ReadModels;
using OrganizationsDomain;

namespace OrganizationsApplication.Persistence;

public interface IOrganizationRepository : IApplicationRepository
{
    Task<Result<Optional<OrganizationRoot>, Error>> FindByAvatarIdAsync(Identifier imageId,
        CancellationToken cancellationToken);

    Task<Result<Optional<OrganizationRoot>, Error>> FindByEmailDomainAsync(string emailDomain,
        CancellationToken cancellationToken);

    Task<Result<OrganizationRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<OrganizationRoot, Error>> SaveAsync(OrganizationRoot organization, CancellationToken cancellationToken);

    Task<Result<QueryResults<Organization>, Error>> SearchAllReferralsAsync(SearchOptions searchOptions,
        CancellationToken cancellationToken);
}