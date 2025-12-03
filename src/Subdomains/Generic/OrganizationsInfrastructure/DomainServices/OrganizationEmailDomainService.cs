using Domain.Common.ValueObjects;
using OrganizationsApplication.Persistence;
using OrganizationsDomain.DomainServices;

namespace OrganizationsInfrastructure.DomainServices;

public class OrganizationEmailDomainService : IOrganizationEmailDomainService
{
    private readonly IOrganizationRepository _repository;

    public OrganizationEmailDomainService(IOrganizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> EnsureUniqueAsync(string emailDomain, Identifier organizationId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByEmailDomainAsync(emailDomain, CancellationToken.None);
        if (retrieved.IsFailure)
        {
            return false;
        }

        var domain = retrieved.Value;
        if (domain.HasValue)
        {
            return domain.Value.Id.Equals(organizationId);
        }

        return true;
    }
}