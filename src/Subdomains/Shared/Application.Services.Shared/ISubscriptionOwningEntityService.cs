using Application.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service for asserting the permissions of an owning entity of a billing subscription.
/// </summary>
public interface ISubscriptionOwningEntityService
{
    /// <summary>
    ///     Whether the subscription can be canceled by the <see cref="cancellerId" />
    /// </summary>
    Task<Result<Permission, Error>> CanCancelSubscriptionAsync(ICallerContext caller, string id, string cancellerId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Whether the subscription plan can be changed by the <see cref="modifierId" />
    /// </summary>
    Task<Result<Permission, Error>> CanChangeSubscriptionPlanAsync(ICallerContext caller, string id, string modifierId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Whether the subscription can be transferred from the <see cref="transfererId" /> to the <see cref="transfereeId" />
    /// </summary>
    Task<Result<Permission, Error>> CanTransferSubscriptionAsync(ICallerContext caller, string id, string transfererId,
        string transfereeId, CancellationToken cancellationToken);

    /// <summary>
    ///     Whether the subscription can be unsubscribed by the <see cref="unsubscriberId" />
    /// </summary>
    Task<Result<Permission, Error>> CanUnsubscribeAsync(ICallerContext caller, string id, string unsubscriberId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Whether the subscription can be viewed by the <see cref="viewerId" />
    /// </summary>
    Task<Result<Permission, Error>> CanViewSubscriptionAsync(ICallerContext caller, string id, string viewerId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the entity for the specified id
    /// </summary>
    Task<Result<OwningEntity, Error>> GetEntityAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);
}

public class OwningEntity
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public required string Type { get; set; }
}