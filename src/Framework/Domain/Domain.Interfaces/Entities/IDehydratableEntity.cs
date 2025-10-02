using Common;
using QueryAny;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines an entity that can persist its state to a set of properties
/// </summary>
public interface IDehydratableEntity : IIdentifiableEntity, IQueryableEntity
{
    Optional<bool> IsDeleted { get; }

    Optional<DateTime> LastPersistedAtUtc { get; }

    HydrationProperties Dehydrate();
}