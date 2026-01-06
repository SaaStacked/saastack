using Domain.Common.Identity;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a <see cref="IIdentifierFactory" /> that manages identifiers for aggregates in subdomains for API hosts
/// </summary>
public sealed class ApiHostIdentifierFactory : AggregateNamePrefixedIdentifierFactory
{
    internal ApiHostIdentifierFactory(IDictionary<Type, string> aggregatePrefixes) : base(aggregatePrefixes)
    {
    }
}