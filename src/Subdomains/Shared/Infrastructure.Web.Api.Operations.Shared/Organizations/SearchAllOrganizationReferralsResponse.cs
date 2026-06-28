using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

public class SearchAllOrganizationReferralsResponse : SearchResponse
{
    public List<OrganizationWithReferralCode> Organizations { get; set; } = new();
}