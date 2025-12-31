using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;
using DomainEventsSharedMarker = Domain.Events.Shared.DomainEventsSharedMarker;

namespace Infrastructure.Eventing.Common;

/// <summary>
///     Provides a <see cref="IEventSourcedChangeEventMigrator" /> that migrates a previous version of domain events to
///     a new version of the domain event class with a collection of <see cref="Mappings" />.
///     Note: This migrator expects that all possible domain event classes, previous version and present version,
///     are in the same AppDomain as this class, and present in the <see cref="DomainEventsSharedMarker" /> assembly.
/// </summary>
public class ChangeEventTypeMigrator : IEventSourcedChangeEventMigrator
{
    private readonly Dictionary<string, Type> _eventTypeMappings;

    /// <summary>
    ///     Creates a new migrator with mappings from previous event classes that are still present in the codebase
    /// </summary>
    public ChangeEventTypeMigrator(Dictionary<Type, Type> eventTypeMappings)
    {
        _eventTypeMappings = eventTypeMappings.ToDictionary(pair => pair.Key.AssemblyQualifiedName!,
            pair => pair.Value);
    }

    /// <summary>
    ///     Creates a new migrator with mappings from second generation previous event classes that are no longer present in
    ///     the codebase
    /// </summary>
    public ChangeEventTypeMigrator(Dictionary<string, Type> typeNameMappings)
    {
        _eventTypeMappings = typeNameMappings.ToDictionary(pair => pair.Key,
            pair => pair.Value);
    }

    public IReadOnlyDictionary<string, Type> Mappings => _eventTypeMappings;

    public Result<IDomainEvent, Error> Rehydrate(string eventId, string eventJson,
        string eventTypeAssemblyQualifiedName)
    {
        var eventTypeName = eventTypeAssemblyQualifiedName;
        if (_eventTypeMappings.ContainsKey(eventTypeName))
        {
            eventTypeName = _eventTypeMappings[eventTypeAssemblyQualifiedName].AssemblyQualifiedName!;
        }

        try
        {
            var eventType = Type.GetType(eventTypeName);
            if (eventType.NotExists())
            {
                return Error.RuleViolation(
                    Resources.ChangeEventMigrator_UnknownType.Format(eventId, eventTypeAssemblyQualifiedName));
            }

            // Serialize JSON into the new type
            return new Result<IDomainEvent, Error>(eventJson.FromEventJson(eventType));
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                Resources.ChangeEventMigrator_FailedToDeserialize.Format(eventId,
                    eventTypeAssemblyQualifiedName, ex.Message));
        }
    }

    public Result<IDomainEvent, Error> Rehydrate(string eventId, IDomainEvent domainEvent)
    {
        var json = domainEvent.ToEventJson();
        var typeFullName = domainEvent.GetType().AssemblyQualifiedName!;
        return Rehydrate(eventId, json, typeFullName);
    }
}