using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderPlanFeature : ValueObjectBase<ProviderPlanFeature>
{
    public static Result<ProviderPlanFeature, Error> Create(string description, bool isIncluded = true)
    {
        if (description.IsInvalidParameter(Validations.Subscriptions.Features.Description, nameof(description),
                Resources.FeatureDefinition_InvalidDescription, out var error1))
        {
            return error1;
        }

        return new ProviderPlanFeature(description, isIncluded);
    }

    private ProviderPlanFeature(string description, bool isIncluded)
    {
        Description = description;
        IsIncluded = isIncluded;
    }

    public string Description { get; }

    public bool IsIncluded { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPlanFeature> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderPlanFeature(
                parts[0],
                parts[1].Value.ToBool());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Description, IsIncluded];
    }
}