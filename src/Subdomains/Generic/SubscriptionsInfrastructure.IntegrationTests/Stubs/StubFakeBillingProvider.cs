using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;
using SubscriptionsInfrastructure.Api._3rdParties;

namespace SubscriptionsInfrastructure.IntegrationTests.Stubs;

/// <summary>
///     A fake provider to help testing various common billing provider scenarios.
///     Works with the <see cref="FakeBillingProviderApi" /> to update payment methods out of band
///     For this provider:
///     1. Has a customerId (from external provider system)
///     2. Supports adding a payment method
///     3. Supports different pricing tiers based on planId 
///     4. Supports canceling immediately or at end of period
///     5. Defines a single billing period of once a month
/// </summary>
public class StubFakeBillingProvider : IBillingProvider
{
    public StubFakeBillingProvider()
    {
        StateInterpreter = new StubFakeBillingProviderStateInterpreter();
        GatewayService = new StubFakeBillingGatewayService();
    }

    public IBillingGatewayService GatewayService { get; }

    public string ProviderName => StateInterpreter.ProviderName;

    public IBillingStateInterpreter StateInterpreter { get; }
}

public class StubFakeBillingProviderStateInterpreter : IBillingStateInterpreter
{
    public Result<string, Error> GetBuyerReference(BillingProvider current)
    {
        return current.State[FakeBillingProviderConstants.MetadataProperties.CustomerId];
    }

    public Result<ProviderSubscription, Error> GetSubscriptionDetails(BillingProvider current)
    {
        var paymentMethod = ProviderPaymentMethod.Empty;
        if (current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.PaymentMethodId,
                out var paymentMethodId))
        {
            paymentMethod = ProviderPaymentMethod.Create(BillingPaymentMethodType.Card,
                BillingPaymentMethodStatus.Valid, Optional<DateOnly>.None).Value;
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
            if (current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.IsCancelled,
                    out var isCancelled))
            {
                if (isCancelled == true.ToString())
                {
                    if (current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.WhenCanceled,
                            out var cancelWhen))
                    {
                        if (cancelWhen == CancelWhen.Immediately.ToString())
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
    public const string InitialPlanId = "apaidtrial";

    public Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAsync(ICallerContext caller,
        CancelSubscriptionOptions options, BillingProvider provider,
        CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State)
        {
            [FakeBillingProviderConstants.MetadataProperties.IsCancelled] = true.ToString(),
            [FakeBillingProviderConstants.MetadataProperties.WhenCanceled] =
                options.CancelWhen == CancelSubscriptionSchedule.Immediately
                    ? CancelWhen.Immediately.ToString()
                    : CancelWhen.EndOfPeriod.ToString()
        };

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    public Task<Result<SubscriptionMetadata, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        ChangePlanOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State)
        {
            [FakeBillingProviderConstants.MetadataProperties.PlanId] = options.PlanId
        };
        metadata.Remove(FakeBillingProviderConstants.MetadataProperties.IsCancelled);
        metadata.Remove(FakeBillingProviderConstants.MetadataProperties.WhenCanceled);

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
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
        metadata.Remove(FakeBillingProviderConstants.MetadataProperties.IsCancelled);

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
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