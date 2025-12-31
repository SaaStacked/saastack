using Application.Persistence.Interfaces;
using Common;

namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines relay of domain events
/// </summary>
public interface IDomainEventConsumerRelay
{
    /// <summary>
    ///     Relays the specified <see cref="changeEvent" /> to the specified <see cref="registration" />
    /// </summary>
    Task<Result<Error>> RelayDomainEventAsync(EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken);
}