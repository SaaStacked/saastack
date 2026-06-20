using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderPlanFeatureSection : ValueObjectBase<ProviderPlanFeatureSection>
{
    public static Result<ProviderPlanFeatureSection, Error> Create(string name,
        IReadOnlyList<ProviderPlanFeature> features)
    {
        if (name.IsInvalidParameter(Validations.Subscriptions.Features.Name, nameof(name),
                Resources.ProviderPlanFeatureSection_InvalidName, out var error1))
        {
            return error1;
        }

        if (features.IsInvalidParameter(items => items.HasAny(), nameof(features),
                Resources.ProviderPlanFeatureSection_EmptyItems, out var error2))
        {
            return error2;
        }

        var feats = ProviderPlanFeatures.Create(features);
        if (feats.IsFailure)
        {
            return feats.Error;
        }

        return new ProviderPlanFeatureSection(name, feats.Value);
    }

    private ProviderPlanFeatureSection(string name,
        ProviderPlanFeatures features)
    {
        Name = name;
        Features = features;
    }

    public ProviderPlanFeatures Features { get; }

    public string Name { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPlanFeatureSection> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderPlanFeatureSection(parts[0], ProviderPlanFeatures.Rehydrate()(parts[1], container));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Name, Features];
    }
}