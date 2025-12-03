using Domain.Common.ValueObjects;

namespace OrganizationsDomain.DomainServices;

/// <summary>
///     Defines a domain service to manage email domains for organizations
/// </summary>
public interface IOrganizationEmailDomainService
{
    /// <summary>
    ///     Whether the specified <see cref="emailDomain" /> is unique across all organizations.
    /// </summary>
    Task<bool> EnsureUniqueAsync(string emailDomain, Identifier organizationId, CancellationToken cancellationToken);
}