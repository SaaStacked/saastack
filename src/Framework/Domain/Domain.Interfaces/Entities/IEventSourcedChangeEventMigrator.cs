using Common;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines a migrator for migrating older/obsolete versions of <see cref="IDomainEvent" /> that no longer
///     exist in the codebase, or have been changed since being persisted to the Event Store.
///     Note: This migrator uses <see cref="Mappings" /> definitions of the fully qualified name (AssemblyQualifiedName) of
///     the original event type to the new event type.
///     Note: If you have several versions to migrate, you should migrate from the persisted version to the latest version.
/// </summary>
public interface IEventSourcedChangeEventMigrator
{
    /// <summary>
    ///     Defines the mappings from the original event type (AssemblyQualifiedName) to the new/upgraded event type.
    ///     Note: The original event type may or may no longer exist in the codebase. The new event type must exist in the
    ///     codebase.
    /// </summary>
    public IReadOnlyDictionary<string, Type> Mappings { get; }

    /// <summary>
    ///     Rehydrates an instance of a <see cref="IDomainEvent" /> from the specified <see cref="eventJson" />,
    ///     into an instance of the type defined by <see cref="eventTypeAssemblyQualifiedName" />.
    ///     If a mapping exists for the <see cref="eventTypeAssemblyQualifiedName" />, then the event is rehydrated
    ///     into an instance of the mapped type.
    /// </summary>
    Result<IDomainEvent, Error> Rehydrate(string eventId, string eventJson, string eventTypeAssemblyQualifiedName);

    /// <summary>
    ///     Rehydrates an instance of the specified <see cref="IDomainEvent" /> from its own JSON representation.
    /// </summary>
    Result<IDomainEvent, Error> Rehydrate(string eventId, IDomainEvent domainEvent);
}