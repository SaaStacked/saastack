using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;
using SubscriptionsInfrastructure.Api._3rdParties;

namespace SubscriptionsInfrastructure.IntegrationTests.Stubs;

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
        return !current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.SubscriptionId, out var subscriptionId)
            ? ProviderSubscription.Create(ProviderStatus.Empty)
            : ProviderSubscription.Create(subscriptionId.ToId(), ProviderStatus.Empty,ProviderPlan.Empty, ProviderPlanPeriod.Empty, ProviderInvoice.Default, ProviderPaymentMethod.Empty);
    }

    public Result<Optional<string>, Error> GetSubscriptionReference(BillingProvider current)
    {
        return current.State.TryGetValue(FakeBillingProviderConstants.MetadataProperties.SubscriptionId, out var subscriptionId)
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
    private const string InitialPlanId = "apaidtrial";

    public Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAsync(ICallerContext caller,
        CancelSubscriptionOptions options, BillingProvider provider,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata(provider.State));
    }

    public Task<Result<SubscriptionMetadata, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        ChangePlanOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State)
        {
            [FakeBillingProviderConstants.MetadataProperties.PlanId] = options.PlanId
        };

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

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    private static string CreateSubscriptionId()
    {
        return Guid.NewGuid().ToString("N");
    }
}