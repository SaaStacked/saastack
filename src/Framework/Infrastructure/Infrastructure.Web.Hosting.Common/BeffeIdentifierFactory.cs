using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Validations;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a <see cref="IIdentifierFactory" /> that only validates aggregate identifiers, used only by BEFFE hosts.
///     Essentially, this identifier factory is used to validate identifiers (in form only),
///     but should never be asked to create them.
/// </summary>
public sealed class BeffeIdentifierFactory : IIdentifierFactory
{
    /// <summary>
    ///     Does not create an identifiers
    /// </summary>
    public Result<Identifier, Error> Create(IIdentifiableEntity entity)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Whether the identifier is of the form expected (see <see cref="AggregateNamePrefixedIdentifierFactory" />),
    ///     without checking whether the prefixes are registered
    /// </summary>
    public bool IsValid(Identifier value)
    {
        var id = value.ToString();
        return CommonValidations.Identifier.Matches(id);
    }
}