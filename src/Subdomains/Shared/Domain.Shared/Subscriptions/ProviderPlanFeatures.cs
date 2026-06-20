using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderPlanFeatures : SingleValueObjectBase<ProviderPlanFeatures, List<ProviderPlanFeature>>
{
    public static ProviderPlanFeatures Empty = new([]);

    public static Result<ProviderPlanFeatures, Error> Create(IReadOnlyList<ProviderPlanFeature> features)
    {
        if (features.IsInvalidParameter(items => items.HasAny(), nameof(features),
                Resources.ProviderPlanFeatures_EmptyItems, out var error))
        {
            return error;
        }

        return new ProviderPlanFeatures(features.ToList());
    }

    private ProviderPlanFeatures(List<ProviderPlanFeature> items) : base(items)
    {
    }

    public IReadOnlyList<ProviderPlanFeature> Items => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPlanFeatures> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            return new ProviderPlanFeatures(
                items
                    .Where(item => item.HasValue)
                    .Select(item => ProviderPlanFeature.Rehydrate()(item, container))
                    .ToList());
        };
    }
}