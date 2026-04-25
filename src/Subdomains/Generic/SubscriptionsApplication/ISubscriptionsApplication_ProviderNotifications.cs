using Application.Interfaces;
using Common;

namespace SubscriptionsApplication;

public partial interface ISubscriptionsApplication
{
    Task<Result<SubscriptionMetadata, Error>> GetProviderStateForBuyerAsync(ICallerContext caller,
        string buyerReference, CancellationToken cancellationToken);

    Task<Result<SubscriptionMetadata, Error>> GetProviderStateForSubscriptionAsync(ICallerContext caller,
        string subscriptionReference, CancellationToken cancellationToken);

    Task<Result<Error>> NotifyBuyerDetailsChangedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken);

    Task<Result<Error>> NotifyBuyerPaymentMethodChangedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken);

    Task<Result<Error>> NotifyBuyerSubscriptionAddedWithPaymentMethodAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken);

    Task<Result<Error>> NotifySubscriptionCanceledAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken);

    Task<Result<Error>> NotifySubscriptionDetailsChangedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken);
}