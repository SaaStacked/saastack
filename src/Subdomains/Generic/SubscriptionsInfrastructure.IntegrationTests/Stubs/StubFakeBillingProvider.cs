#if TESTINGONLY
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using SubscriptionsInfrastructure.Api._3rdParties;

namespace SubscriptionsInfrastructure.IntegrationTests.Stubs;

/// <summary>
///     A fake billing provider to help testing various common billing provider scenarios.
///     Works with the <see cref="FakeBillingProviderApi" /> to update payment methods out of band
///     For this provider:
///     1. Has a customerId (from external provider system)
///     2. Supports adding a payment method
///     3. Supports different pricing tiers based on planId
///     4. Supports canceling immediately or at end of period
///     5. Defines a single billing period of once a month
///     6. Defines no trial
/// </summary>
public class StubFakeBillingProvider : IBillingProvider
{
    public StubFakeBillingProvider()
    {
        Capabilities = new BillingProviderCapabilities
        {
            TrialManagement = TrialManagementOptions.SelfManaged
        };
        StateInterpreter = new StubFakeBillingProviderStateInterpreter(Capabilities);
        GatewayService = new StubFakeBillingGatewayService(Capabilities);
    }

    public BillingProviderCapabilities Capabilities { get; }

    public IBillingGatewayService GatewayService { get; }

    public string ProviderName => StateInterpreter.ProviderName;

    public IBillingStateInterpreter StateInterpreter { get; }

    public void Reset()
    {
        GatewayService.As<StubFakeBillingGatewayService>().Reset();
    }
}

public class StubFakeBillingProviderStateInterpreter : IBillingStateInterpreter
{
    public StubFakeBillingProviderStateInterpreter(BillingProviderCapabilities capabilities)
    {
        Capabilities = capabilities;
    }

    public BillingProviderCapabilities Capabilities { get; }

    public Result<string, Error> GetBuyerReference(BillingProvider current)
    {
        return current.State[FakeBillingProviderConstants.MetadataProperties.CustomerId];
    }

    public Result<ProviderSubscription, Error> GetSubscriptionDetails(BillingProvider current)
    {
        var paymentMethod = ProviderPaymentMethod.Empty;
        if (current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.PaymentMethodId,
                out _))
        {
            paymentMethod = ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                BillingPaymentMethodStatus.Valid, Optional<DateOnly>.None, Optional<string>.None).Value;
        }

        var plan = ProviderPlan.Empty;
        if (current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.PlanId,
                out var planId))
        {
            var tier = planId switch
            {
                StubFakeBillingGatewayService.InitialPlanId => BillingSubscriptionTier.Standard,
                "apaid2" => BillingSubscriptionTier.Professional,
                _ => BillingSubscriptionTier.Unsubscribed
            };
            plan = ProviderPlan.Create(planId, tier).Value;
        }

        if (current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.SubscriptionId,
                out var subscriptionId))
        {
            var providerStatus = ProviderStatus.Empty;
            if (current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.IsCanceled,
                    out var isCanceled))
            {
                if (isCanceled == true.ToString())
                {
                    if (current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.WhenCanceled,
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
        return current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.SubscriptionId,
            out var subscriptionId)
            ? subscriptionId.ToOptional()
            : Optional<string>.None;
    }

    public string ProviderName => FakeBillingProviderConstants.ProviderName;

    public Result<BillingProvider, Error> SetInitialProviderState(BillingProvider provider)
    {
        // do nothing
        return provider;
    }
}

public class StubFakeBillingGatewayService : IBillingGatewayService
{
    public const string InitialPlanId = "1234567890";

    public StubFakeBillingGatewayService(BillingProviderCapabilities capabilities)
    {
        Capabilities = capabilities;
    }

    public Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAsync(ICallerContext caller,
        CancelSubscriptionOptions options, BillingProvider provider,
        CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State)
        {
            [FakeBillingProviderConstants.MetadataProperties.IsCanceled] = true.ToString(),
            [FakeBillingProviderConstants.MetadataProperties.WhenCanceled] =
                options.CancelWhen == CancelSubscriptionSchedule.Immediately
                    ? nameof(CancelWhen.Immediately)
                    : nameof(CancelWhen.EndOfPeriod)
        };

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    public BillingProviderCapabilities Capabilities { get; }

    public Task<Result<SubscriptionMetadata, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        ChangePlanOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State)
        {
            [FakeBillingProviderConstants.MetadataProperties.PlanId] = options.PlanId
        };
        metadata.Remove(FakeBillingProviderConstants.MetadataProperties.IsCanceled);
        metadata.Remove(FakeBillingProviderConstants.MetadataProperties.WhenCanceled);

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    public Task<Result<Error>> HandleTrialScheduledEventAsync(ICallerContext caller, SubscriptionBuyer buyer,
        TrialScheduledEvent trialEvent, BillingProvider provider, CancellationToken cancellationToken)
    {
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
            { FakeBillingProviderConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId }
        });
    }

    public Task<Result<SubscriptionMetadata, Error>> ReSyncSubscriptionAsync(ICallerContext caller,
        BillingProvider provider, CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State);
        metadata.Remove(FakeBillingProviderConstants.MetadataProperties.IsCanceled);
        metadata.Remove(FakeBillingProviderConstants.MetadataProperties.WhenCanceled);

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
            { FakeBillingProviderConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId },
            { FakeBillingProviderConstants.MetadataProperties.SubscribedAt, DateTime.UtcNow.ToIso8601() },
            { FakeBillingProviderConstants.MetadataProperties.SubscriptionId, CreateSubscriptionId() },
#if TESTINGONLY
            { FakeBillingProviderConstants.MetadataProperties.PlanId, options.PlanId ?? InitialPlanId },
#endif
        });
    }

    public Task<Result<SubscriptionMetadata, Error>> TransferSubscriptionAsync(ICallerContext caller,
        TransferSubscriptionOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        var planId = options.PlanId ?? provider.State[FakeBillingProviderConstants.MetadataProperties.PlanId];

        var metadata = new SubscriptionMetadata(provider.State)
        {
            [FakeBillingProviderConstants.MetadataProperties.PlanId] = planId
        };
        metadata.Remove(FakeBillingProviderConstants.MetadataProperties.IsCanceled);

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    public void Reset()
    {
    }

    private static string CreateSubscriptionId()
    {
        return Guid.NewGuid().ToString("N");
    }
}

public enum CancelWhen
{
    Immediately,
    EndOfPeriod
}
#endif