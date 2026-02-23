using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace OrganizationsDomain;

public sealed class BranchesSchema : SingleValueObjectBase<BranchesSchema, List<BranchSchema>>
{
    public static Result<BranchesSchema, Error> Create(IReadOnlyList<BranchSchema> value)
    {
        return new BranchesSchema(value.ToList());
    }

    private BranchesSchema(List<BranchSchema> value) : base(value)
    {
    }

    public List<BranchSchema> Items => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<BranchesSchema> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            var branches = items.Select(item => BranchSchema.Rehydrate()(item, container));

            return new BranchesSchema(branches.ToList());
        };
    }
}