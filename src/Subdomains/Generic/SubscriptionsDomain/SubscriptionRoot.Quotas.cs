using Common;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;

namespace SubscriptionsDomain;

/// <summary>
///     This aspect of a the <see cref="SubscriptionRoot" /> manages the metering and quotas for providers that cannot
///     manage their own, when the subscription is created.
/// </summary>
#pragma warning disable SAASDDD014
#pragma warning disable SAASDDD010
#pragma warning disable SAASDDD018
partial class SubscriptionRoot
#pragma warning restore SAASDDD018
#pragma warning restore SAASDDD010
#pragma warning restore SAASDDD014
{
    public delegate Task<Result<SubscriptionMetadata, Error>> IncrementUsageAction(SubscriptionRoot subscription);

    public delegate Task<Result<Error>> PrepareTierQuotasAction(SubscriptionRoot subscription,
        Optional<BillingSubscriptionTier> fromTier, BillingSubscriptionTier toTier);

    public Optional<ProviderTierQuotas> ManagedQuotas { get; private set; }

    public async Task<Result<Error>> IncrementMeteredUsageAsync(IBillingStateInterpreter interpreter, string eventName,
        IncrementUsageAction onIncrement)
    {
        var verified = VerifyProviderIsSameAsInstalled(interpreter);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        var allowedEvents = interpreter.Capabilities.MeteredEvents;
        if (!allowedEvents.Contains(eventName))
        {
            return Result.Ok;
        }

        var incremented = await onIncrement(this);
        if (incremented.IsFailure)
        {
            return incremented.Error;
        }

        return Result.Ok;
    }

#if TESTINGONLY
    public void TestingOnly_SetManagedQuotas(ProviderTierQuotas quotas)
    {
        ManagedQuotas = quotas;
    }
#endif

    private async Task<Result<Error>> ResyncTrialQuotasAsync(IBillingStateInterpreter interpreter,
        Optional<BillingSubscriptionTier> fromTier, BillingSubscriptionTier toTier,
        PrepareTierQuotasAction onPrepareTierQuotas)
    {
        if (interpreter.Capabilities.QuotaManagement is not ManagementOptions.RequiresManaged)
        {
            return Result.Ok;
        }

        if (!ManagedQuotas.HasValue)
        {
            return Result.Ok;
        }

        var quotasSaved = await onPrepareTierQuotas(this, fromTier, toTier);
        if (quotasSaved.IsFailure)
        {
            return quotasSaved.Error;
        }

        return Result.Ok;
    }
}