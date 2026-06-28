using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Fetches the organizations that have referral codes
/// </summary>
[Route("/organizations/{Id}/referrals", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class
    SearchAllOrganizationReferralsRequest : UnTenantedSearchRequest<SearchAllOrganizationReferralsRequest,
    SearchAllOrganizationReferralsResponse>
{
}