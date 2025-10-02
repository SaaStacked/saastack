using Application.Persistence.Common;
using Common;
using Domain.Shared.Organizations;
using QueryAny;

namespace OrganizationsApplication.Persistence.ReadModels;

[EntityName("Organization")]
public class Organization : ReadModelEntity
{
    public Optional<string> AvatarImageId { get; set; }

    public Optional<string> AvatarUrl { get; set; }

    public Optional<string> BillingSubscriberId { get; set; }

    public Optional<string> BillingSubscriptionId { get; set; }

    public Optional<string> CreatedById { get; set; }

    public Optional<string> Name { get; set; }

    public Optional<OrganizationOwnership> Ownership { get; set; }

    public Optional<string> RegisteredRegion { get; set; }
}