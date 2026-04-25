using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Defines a <see cref="IUsageDeliveryService" /> that delivers usage events to a primary
///     <see cref="IUsageDeliveryService" />,
///     and also filters usage events, for forwarding to a subscription for metered usage.
/// </summary>
public class MeteredBillingUsageDeliveryService : IUsageDeliveryService
{
    private readonly IReadOnlyList<string> _meteredEvents;
    private readonly IUsageDeliveryService _primaryService;
    private readonly IRecorder _recorder;
    private readonly ISubscriptionsService _subscriptionsService;

    public MeteredBillingUsageDeliveryService(IRecorder recorder, IUsageDeliveryService primaryService,
        IBillingProvider billingProvider, ISubscriptionsService subscriptionsService)
    {
        _recorder = recorder;
        _primaryService = primaryService;
        _subscriptionsService = subscriptionsService;
        _meteredEvents = billingProvider.Capabilities.MeteredEvents;
    }

    public async Task<Result<Error>> DeliverAsync(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional = null, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task<Result<Error>>>
        {
            _primaryService.DeliverAsync(caller, forId, eventName, additional, cancellationToken)
        };

        var tenantId = caller.TenantId;
        if (tenantId.HasValue)
        {
            if (ShouldMeterEvent(eventName))
            {
                tasks.Add(IncrementUsage());
            }
        }

        return await Tasks.WhenAllAsync(tasks.ToArray());

        async Task<Result<Error>> IncrementUsage()
        {
            var incremented =
                await _subscriptionsService.IncrementSubscriptionUsageAsync(caller, tenantId, eventName,
                    cancellationToken);
            if (incremented.IsFailure)
            {
                return incremented.Error;
            }

            _recorder.TraceInformation(caller.ToCall(), "Incremented {MeterName} usage", eventName);
            return Result.Ok;
        }
    }

    private bool ShouldMeterEvent(string eventName)
    {
        return _meteredEvents.ContainsIgnoreCase(eventName);
    }
}