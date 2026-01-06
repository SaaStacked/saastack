using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Gets the user's consent status for an OAuth2/Open ID Connect client
/// </summary>
[Route("/oauth2/clients/{Id}/consent/status", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class
    GetOAuth2ClientConsentStatusForCallerRequest : UnTenantedRequest<GetOAuth2ClientConsentStatusForCallerRequest,
    GetOAuth2ClientConsentStatusResponse>
{
    [Required] public string? Id { get; set; }

    [Required] public string? Scope { get; set; }
}