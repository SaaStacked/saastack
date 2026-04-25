using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace SubscriptionsApplication;

public partial interface ISubscriptionsApplication
{
    Task<Result<SubscriptionWithPlan, Error>> CancelSubscriptionAsync(ICallerContext caller, string owningEntityId,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> ChangePlanAsync(ICallerContext caller, string owningEntityId,
        string planId,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<SubscriptionWithPlan, Error>> ConvertSubscriptionAsync(ICallerContext caller, string owningEntityId,
        CancellationToken cancellationToken);
#endif

    Task<Result<bool, Error>> DeliverSubscriptionTrialEventAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<Error>>
        DrainAllSubscriptionTrialEventsAsync(ICallerContext caller, CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    Task<Result<SubscriptionWithPlan, Error>> ExpireTrialAsync(ICallerContext caller, string owningEntityId,
        CancellationToken cancellationToken);
#endif

    Task<Result<SearchResults<SubscriptionToMigrate>, Error>> ExportSubscriptionsToMigrateAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> ForceCancelSubscriptionAsync(ICallerContext caller, string owningEntityId,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionByIdAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionByOwningEntityIdAsync(ICallerContext caller,
        string owningEntityId,
        CancellationToken cancellationToken);

    Task<Result<Error>> IncrementSubscriptionUsageAsync(ICallerContext caller, string owningEntityId, string eventName,
        CancellationToken cancellationToken);

    Task<Result<PricingPlans, Error>> ListPricingPlansAsync(ICallerContext caller, CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> MigrateSubscriptionAsync(ICallerContext caller, string? owningEntityId,
        string providerName, Dictionary<string, string> providerState, CancellationToken cancellationToken);

    Task<Result<SearchResults<Invoice>, Error>> SearchSubscriptionHistoryAsync(ICallerContext caller,
        string owningEntityId, DateTime? fromUtc, DateTime? toUtc, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> TransferSubscriptionAsync(ICallerContext caller, string owningEntityId,
        string billingAdminId, CancellationToken cancellationToken);
}