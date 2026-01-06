using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Consent for the user to authorize the OAuth2/Open ID Connect client to access their data
/// </summary>
[Route("/oauth2/clients/{Id}/consent", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class ConsentOAuth2ClientRequest : UnTenantedRequest<ConsentOAuth2ClientRequest,
    ConsentOAuth2ClientResponse>
{
    [Required] public bool Consented { get; set; }

    [Required] public string? Id { get; set; }

    [Required] public string? RedirectUri { get; set; }

    [Required] public string? Scope { get; set; }

    public string? State { get; set; }
}