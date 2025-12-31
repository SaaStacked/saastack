using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;
using DomainEventsSharedMarker = Domain.Events.Shared.DomainEventsSharedMarker;

namespace Infrastructure.Eventing.Common.Extensions;

public static class NotificationExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="DomainEventNotification" /> to a <see cref="IDomainEvent" />
    ///     Note: This conversion expects that all possible domain event classes are in the same AppDomain as this class,
    ///     and present in the <see cref="DomainEventsSharedMarker" /> assembly.
    /// </summary>
    public static Result<IDomainEvent, Error> ToDomainEvent(this DomainEventNotification notification)
    {
        var eventJson = notification.EventJsonData;
        var eventTypeFullName = notification.EventTypeFullName;
        var eventType = Type.GetType(eventTypeFullName)!;

        if (eventType.NotExists())
        {
            return Error.Unexpected(
                Resources.EventingExtensions_ToDomainEvent_UnknownType.Format(eventTypeFullName));
        }

        try
        {
            var @event = eventJson.FromEventJson(eventType);
            return new Result<IDomainEvent, Error>(@event);
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                Resources.EventingExtensions_ToDomainEvent_FailedToDeserialize.Format(eventTypeFullName,
                    ex.Message));
        }
    }
}