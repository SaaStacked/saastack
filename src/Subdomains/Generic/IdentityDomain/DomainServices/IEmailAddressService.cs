using Domain.Common.ValueObjects;
using Domain.Shared;

namespace IdentityDomain.DomainServices;

/// <summary>
///     Defines services for email addresses
/// </summary>
public interface IEmailAddressService
{
    /// <summary>
    ///     Whether the specified <see cref="emailAddress" /> is unique across all end users.
    /// </summary>
    Task<bool> EnsureUniqueAsync(EmailAddress emailAddress, Identifier userId ,CancellationToken cancellationToken);
}