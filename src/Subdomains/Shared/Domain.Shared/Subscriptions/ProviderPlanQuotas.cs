using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class
    ProviderPlanQuotas : SingleValueObjectBase<ProviderPlanQuotas, Dictionary<string, ProviderPlanQuota>>
{
    public static readonly ProviderPlanQuotas Empty = new(new Dictionary<string, ProviderPlanQuota>());

    public IReadOnlyDictionary<string, ProviderPlanQuota> Items => Value;

    public static Result<ProviderPlanQuotas, Error> Create(string id, ProviderPlanQuota definition)
    {
        return new ProviderPlanQuotas(new Dictionary<string, ProviderPlanQuota> { { id, definition } });
    }

    public static Result<ProviderPlanQuotas, Error> Create(IReadOnlyDictionary<string, ProviderPlanQuota> quotas)
    {
        if (quotas.IsInvalidParameter(items => items.HasAny(), nameof(quotas),
                Resources.ProviderPlanQuotas_EmptyItems, out var error))
        {
            return error;
        }

        return new ProviderPlanQuotas(quotas.ToDictionary());
    }

    private ProviderPlanQuotas(Dictionary<string, ProviderPlanQuota> value) : base(value)
    {
    }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPlanQuotas> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToStringDictionary(property);
            var quotas = items.ToDictionary(item => item.Key,
                item => ProviderPlanQuota.Rehydrate()(item.Value, container));

            return new ProviderPlanQuotas(quotas);
        };
    }
}