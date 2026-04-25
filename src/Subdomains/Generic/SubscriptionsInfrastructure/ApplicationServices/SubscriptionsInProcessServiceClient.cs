using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.ApplicationServices;

public class SubscriptionsInProcessServiceClient : ISubscriptionsService
{
    private readonly Func<ISubscriptionsApplication> _subscriptionsApplicationFactory;
    private ISubscriptionsApplication? _application;

    /// <summary>
    ///     HACK: LazyGetRequiredService and <see cref="Func{TResult}" /> is needed here to avoid the runtime cyclic dependency
    ///     between
    ///     <see cref="ISubscriptionsApplication" /> requiring <see cref="ISubscriptionOwningEntityService" />, which requires
    ///     <see cref="ISubscriptionsService" />
    /// </summary>
    public SubscriptionsInProcessServiceClient(Func<ISubscriptionsApplication> subscriptionsApplicationFactory)
    {
        _subscriptionsApplicationFactory = subscriptionsApplicationFactory;
    }

    public async Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionByIdAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await GetApplication().GetSubscriptionByIdAsync(caller, id, cancellationToken);
    }

    public async Task<Result<Error>> IncrementSubscriptionUsageAsync(ICallerContext caller, string owningEntityId,
        string eventName,
        CancellationToken cancellationToken)
    {
        return await GetApplication()
            .IncrementSubscriptionUsageAsync(caller, owningEntityId, eventName, cancellationToken);
    }

    private ISubscriptionsApplication GetApplication()
    {
        if (_application.NotExists())
        {
            _application = _subscriptionsApplicationFactory();
        }

        return _application;
    }
}