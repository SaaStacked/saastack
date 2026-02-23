using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Retrieves the organization that has been created for the email domain of the caller (if any)
/// </summary>
/// <response code="404">No organization has been created for the email domain of the caller</response>
[Route("/organizations/shared-email", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class GetSharedOrganizationForCallerEmailDomainRequest : UnTenantedRequest<
    GetSharedOrganizationForCallerEmailDomainRequest,
    GetSharedOrganizationForCallerEmailDomainResponse>
{
}