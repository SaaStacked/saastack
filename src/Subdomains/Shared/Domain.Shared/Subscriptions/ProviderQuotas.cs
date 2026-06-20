using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class
    ProviderQuotas : SingleValueObjectBase<ProviderQuotas, Dictionary<BillingSubscriptionTier, ProviderPlanQuotas>>
{
    public static readonly ProviderQuotas Empty = new([]);

    public static Result<ProviderQuotas, Error> Create(
        IReadOnlyDictionary<BillingSubscriptionTier, ProviderPlanQuotas> quotas)
    {
        if (quotas.IsInvalidParameter(items => items.HasAny(), nameof(quotas),
                Resources.ProviderQuotas_EmptyItems, out var error1))
        {
            return error1;
        }

        if (quotas.IsInvalidParameter(HasNoneUnsubscribed, nameof(quotas),
                Resources.ProviderQuotas_UnsubscribedItems.Format(BillingSubscriptionTier.Unsubscribed),
                out var error2))
        {
            return error2;
        }

        return new ProviderQuotas(quotas.ToDictionary());

        static bool HasNoneUnsubscribed(IReadOnlyDictionary<BillingSubscriptionTier, ProviderPlanQuotas> quotas)
        {
            if (quotas.TryGetValue(BillingSubscriptionTier.Unsubscribed, out var unsubscribed))
            {
                return unsubscribed.Items.HasNone();
            }

            return true;
        }
    }

    private ProviderQuotas(Dictionary<BillingSubscriptionTier, ProviderPlanQuotas> items) : base(items)
    {
    }

    public Dictionary<BillingSubscriptionTier, ProviderPlanQuotas> Items => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderQuotas> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToStringDictionary(property);
            var quotas = items.ToDictionary(item => item.Key.ToEnumOrDefault(BillingSubscriptionTier.Unsubscribed),
                item => ProviderPlanQuotas.Rehydrate()(item.Value, container));

            return new ProviderQuotas(quotas);
        };
    }

    [SkipImmutabilityCheck]
    public Optional<ProviderPlanQuotas> ForTier(BillingSubscriptionTier currentTier)
    {
        if (Value.TryGetValue(currentTier, out var quotas))
        {
            return quotas;
        }

        return Optional.None<ProviderPlanQuotas>();
    }

    [SkipImmutabilityCheck]
    public bool HasAny()
    {
        return Value.HasAny();
    }

    [SkipImmutabilityCheck]
    public bool HasNone()
    {
        return Value.HasNone();
    }
}