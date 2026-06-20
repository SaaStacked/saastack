using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderPlanQuota : ValueObjectBase<ProviderPlanQuota>
{
    public static Result<ProviderPlanQuota, Error> Create(string description, long limit = -1,
        BillingSubscriptionQuotaPeriod period = BillingSubscriptionQuotaPeriod.Eternity)
    {
        if (description.IsInvalidParameter(Validations.Subscriptions.Quota.Description, nameof(description),
                Resources.ProviderPlanQuota_InvalidDescription, out var error))
        {
            return error;
        }

        return new ProviderPlanQuota(description, limit, period);
    }

    private ProviderPlanQuota(string description, long limit, BillingSubscriptionQuotaPeriod period)
    {
        Description = description;
        Limit = limit;
        Period = period;
    }

    public string Description { get; }

    /// <summary>
    ///     Returns the limit of the quota. -1 means unlimited
    /// </summary>
    public long Limit { get; }

    public BillingSubscriptionQuotaPeriod Period { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPlanQuota> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderPlanQuota(parts[0], parts[1].Value.ToInt(),
                parts[2].Value.ToEnumOrDefault(BillingSubscriptionQuotaPeriod.Eternity));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Description, Limit, Period];
    }
}

public enum BillingSubscriptionQuotaPeriod
{
    Eternity = 0
}