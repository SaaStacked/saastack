#if TESTINGONLY
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Subscriptions;
using FluentAssertions;

namespace SubscriptionsInfrastructure.IntegrationTests.Stubs;

/// <summary>
///     A stub billing provider to help testing a billing provider that requires a managed trial
///     For this provider:
///     1. Has a customerId (from external provider system)
///     2. Supports adding a payment method
///     3. Supports different pricing tiers based on planId
///     4. Supports canceling immediately or at end of period
///     5. Defines a single billing period of once a month
///     6. Defines a trial period of 7 days, and an event schedule with three events for the active state, expiry state and
///     conversion state,
///     7. Requires the API to manage its trial expiry and schedule of events
/// </summary>
public class StubManagedTrialBillingProvider : IBillingProvider
{
    private const int TrialDurationDays = 7;
    private static TimeSpan _timeMachineOffset = TimeSpan.Zero;

    public StubManagedTrialBillingProvider()
    {
        StateInterpreter = new StubManagedTrialBillingProviderStateInterpreter(GetCapabilities);
        GatewayService = new StubManagedTrialBillingGatewayService(GetCapabilities);
    }

    public TrialScheduledEvent? LastTrialEvent =>
        GatewayService.As<StubManagedTrialBillingGatewayService>().LastTrialEvent;

    public BillingProviderCapabilities Capabilities => GetCapabilities();

    public IBillingGatewayService GatewayService { get; }

    public string ProviderName => StateInterpreter.ProviderName;

    public IBillingStateInterpreter StateInterpreter { get; }

    public void AddPaymentMethod()
    {
        StateInterpreter.As<StubManagedTrialBillingProviderStateInterpreter>().AddPaymentMethod();
    }

    public void Reset()
    {
        _timeMachineOffset = TimeSpan.Zero;
        StateInterpreter.As<StubManagedTrialBillingProviderStateInterpreter>().Reset();
        GatewayService.As<StubManagedTrialBillingGatewayService>().Reset();
    }

    public void TimeTravelPastEndOfTrial()
    {
        //Move ahead to just past the end of the trial period
        _timeMachineOffset = TimeSpan.FromDays(TrialDurationDays);
    }

    private static BillingProviderCapabilities GetCapabilities()
    {
        const int infiniteDays = 99; // never will get delivered, will just sit on the delivery queue
        var zeroDelay =
            (int)TimeSpan.FromDays(0).Add(_timeMachineOffset)
                .TotalDays; //adjusted by time machine for immediate delivery
        var activeEvent1 = TrialScheduledEvent.Create(zeroDelay, "anactiveeventid1",
            TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var activeEvent2 = TrialScheduledEvent.Create(infiniteDays, "anactiveeventid2",
            TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var activeEvent3 = TrialScheduledEvent.Create(infiniteDays, "anactiveeventid3",
            TrialScheduledEventTrack.Active,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var expiredEvent1 = TrialScheduledEvent.Create(zeroDelay, "anexpiredeventid1",
            TrialScheduledEventTrack.Expired,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var expiredEvent2 = TrialScheduledEvent.Create(infiniteDays, "anexpiredeventid2",
            TrialScheduledEventTrack.Expired,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var expiredEvent3 = TrialScheduledEvent.Create(infiniteDays, "anexpiredeventid3",
            TrialScheduledEventTrack.Expired,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var convertedEvent1 = TrialScheduledEvent.Create(zeroDelay, "aconvertedeventid1",
            TrialScheduledEventTrack.Converted,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var convertedEvent2 = TrialScheduledEvent.Create(infiniteDays, "aconvertedeventid2",
            TrialScheduledEventTrack.Converted,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;
        var convertedEvent3 = TrialScheduledEvent.Create(infiniteDays, "aconvertedeventid3",
            TrialScheduledEventTrack.Converted,
            TrialScheduledEventAction.Notification, StringNameValues.Empty).Value;

        var schedule = TrialEventSchedule.Create([
            activeEvent1, activeEvent2, activeEvent3,
            expiredEvent1, expiredEvent2, expiredEvent3,
            convertedEvent1, convertedEvent2, convertedEvent3
        ]).Value;

        return new BillingProviderCapabilities
        {
            TrialManagement = TrialManagementOptions.RequiresManaged,
            ManagedTrialDurationDays = TrialDurationDays,
            ManagedTrialSchedule = schedule
        };
    }
}

public class StubManagedTrialBillingProviderStateInterpreter : IBillingStateInterpreter
{
    private static bool _hasPaymentMethod;
    private readonly Func<BillingProviderCapabilities> _capabilities;

    public StubManagedTrialBillingProviderStateInterpreter(Func<BillingProviderCapabilities> capabilities)
    {
        _capabilities = capabilities;
    }

    public BillingProviderCapabilities Capabilities => _capabilities();

    public Result<string, Error> GetBuyerReference(BillingProvider current)
    {
        return current.State[TriallessBillingProviderConstants.MetadataProperties.CustomerId];
    }

    public Result<ProviderSubscription, Error> GetSubscriptionDetails(BillingProvider current)
    {
        var paymentMethod = ProviderPaymentMethod.Empty;
        if (current.State.TryGetValue(TriallessBillingProviderConstants.MetadataProperties.PaymentMethodId,
                out _) || _hasPaymentMethod)
        {
            paymentMethod = ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                BillingPaymentMethodStatus.Valid, Optional<DateOnly>.None, Optional<string>.None).Value;
        }

        var plan = ProviderPlan.Empty;
        if (current.State.TryGetValue(TriallessBillingProviderConstants.MetadataProperties.PlanId,
                out var planId))
        {
            var tier = planId switch
            {
                StubManagedTrialBillingGatewayService.InitialPlanId => BillingSubscriptionTier.Standard,
                "apaid2" => BillingSubscriptionTier.Professional,
                _ => BillingSubscriptionTier.Unsubscribed
            };
            plan = ProviderPlan.Create(planId, tier).Value;
        }

        if (current.State.TryGetValue(TriallessBillingProviderConstants.MetadataProperties.SubscriptionId,
                out var subscriptionId))
        {
            var providerStatus = ProviderStatus.Empty;
            if (current.State.TryGetValue(TriallessBillingProviderConstants.MetadataProperties.IsCancelled,
                    out var isCancelled))
            {
                if (isCancelled == true.ToString())
                {
                    if (current.State.TryGetValue(TriallessBillingProviderConstants.MetadataProperties.WhenCanceled,
                            out var cancelWhen))
                    {
                        if (cancelWhen == nameof(CancelWhen.Immediately))
                        {
                            providerStatus = ProviderStatus
                                .Create(BillingSubscriptionStatus.Canceled, DateTime.UtcNow, true).Value;
                            plan = ProviderPlan.Create(plan.PlanId, BillingSubscriptionTier.Unsubscribed).Value;
                        }
                        else
                        {
                            providerStatus = ProviderStatus
                                .Create(BillingSubscriptionStatus.Canceling, DateTime.UtcNow.AddMonths(1), true).Value;
                        }
                    }
                }
            }
            else
            {
                providerStatus = ProviderStatus
                    .Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value;
            }

            var planPeriod = ProviderPlanPeriod.Create(1, BillingFrequencyUnit.Month).Value;

            return ProviderSubscription.Create(subscriptionId.ToId(), providerStatus, plan,
                planPeriod, ProviderInvoice.Default, paymentMethod);
        }

        return ProviderSubscription.Create(ProviderStatus.Empty, paymentMethod);
    }

    public Result<Optional<string>, Error> GetSubscriptionReference(BillingProvider current)
    {
        return current.State.TryGetValue(TriallessBillingProviderConstants.MetadataProperties.SubscriptionId,
            out var subscriptionId)
            ? subscriptionId.ToOptional()
            : Optional<string>.None;
    }

    public string ProviderName => TriallessBillingProviderConstants.ProviderName;

    public Result<BillingProvider, Error> SetInitialProviderState(BillingProvider provider)
    {
        // do nothing
        return provider;
    }

    public void AddPaymentMethod()
    {
        _hasPaymentMethod = true;
    }

    public void Reset()
    {
        _hasPaymentMethod = false;
    }
}

public class StubManagedTrialBillingGatewayService : IBillingGatewayService
{
    public const string InitialPlanId = "1234567890";
    private readonly Func<BillingProviderCapabilities> _capabilities;

    public StubManagedTrialBillingGatewayService(Func<BillingProviderCapabilities> capabilities)
    {
        _capabilities = capabilities;
    }

    public TrialScheduledEvent? LastTrialEvent { get; private set; }

    public Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAsync(ICallerContext caller,
        CancelSubscriptionOptions options, BillingProvider provider,
        CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State)
        {
            [TriallessBillingProviderConstants.MetadataProperties.IsCancelled] = true.ToString(),
            [TriallessBillingProviderConstants.MetadataProperties.WhenCanceled] =
                options.CancelWhen == CancelSubscriptionSchedule.Immediately
                    ? nameof(CancelWhen.Immediately)
                    : nameof(CancelWhen.EndOfPeriod)
        };

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    public BillingProviderCapabilities Capabilities => _capabilities();

    public Task<Result<SubscriptionMetadata, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        ChangePlanOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State)
        {
            [TriallessBillingProviderConstants.MetadataProperties.PlanId] = options.PlanId
        };
        metadata.Remove(TriallessBillingProviderConstants.MetadataProperties.IsCancelled);
        metadata.Remove(TriallessBillingProviderConstants.MetadataProperties.WhenCanceled);

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    public Task<Result<Error>> HandleTrialScheduledEventAsync(ICallerContext caller, SubscriptionBuyer buyer,
        TrialScheduledEvent trialEvent, BillingProvider provider, CancellationToken cancellationToken)
    {
        LastTrialEvent = trialEvent;
        return Task.FromResult(Result.Ok);
    }

    public Task<Result<SubscriptionMetadata, Error>> IncrementMeterAsync(ICallerContext caller, string meterName,
        BillingProvider provider, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(provider.State);
    }

    public Task<Result<PricingPlans, Error>> ListAllPricingPlansAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<PricingPlans, Error>>(new PricingPlans());
    }

    public Task<Result<SubscriptionMetadata, Error>> RestoreBuyerAsync(ICallerContext caller, SubscriptionBuyer buyer,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            { TriallessBillingProviderConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId }
        });
    }

    public Task<Result<SubscriptionMetadata, Error>> ReSyncSubscriptionAsync(ICallerContext caller,
        BillingProvider provider, CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State);
        metadata.Remove(TriallessBillingProviderConstants.MetadataProperties.IsCancelled);
        metadata.Remove(TriallessBillingProviderConstants.MetadataProperties.WhenCanceled);

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    public Task<Result<SearchResults<Invoice>, Error>> SearchAllInvoicesAsync(ICallerContext caller,
        BillingProvider provider,
        DateTime fromUtc, DateTime toUtc, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SearchResults<Invoice>, Error>>(new SearchResults<Invoice>());
    }

    public Task<Result<SubscriptionMetadata, Error>> SubscribeAsync(ICallerContext caller,
        SubscriptionBuyer buyer, SubscribeOptions options, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            { TriallessBillingProviderConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId },
            { TriallessBillingProviderConstants.MetadataProperties.SubscribedAt, DateTime.UtcNow.ToIso8601() },
            { TriallessBillingProviderConstants.MetadataProperties.SubscriptionId, CreateSubscriptionId() },
#if TESTINGONLY
            { TriallessBillingProviderConstants.MetadataProperties.PlanId, options.PlanId ?? InitialPlanId },
#endif
        });
    }

    public Task<Result<SubscriptionMetadata, Error>> TransferSubscriptionAsync(ICallerContext caller,
        TransferSubscriptionOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        var planId = options.PlanId ?? provider.State[TriallessBillingProviderConstants.MetadataProperties.PlanId];

        var metadata = new SubscriptionMetadata(provider.State)
        {
            [TriallessBillingProviderConstants.MetadataProperties.PlanId] = planId
        };
        metadata.Remove(TriallessBillingProviderConstants.MetadataProperties.IsCancelled);

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    public void Reset()
    {
        LastTrialEvent = null;
    }

    private static string CreateSubscriptionId()
    {
        return Guid.NewGuid().ToString("N");
    }
}

public static class TriallessBillingProviderConstants
{
    public const string ProviderName = "trialless_billing_provider";

    public static class MetadataProperties
    {
        public const string CustomerId = "CustomerId";
        public const string IsCancelled = "IsCancelled";
        public const string PaymentMethodId = "PaymentMethodId";
        public const string PlanId = "PlanId";
        public const string SubscribedAt = "SubscribedAt";
        public const string SubscriptionId = "SubscriptionId";
        public const string WhenCanceled = "CancelWhen";
    }
}
#endif