using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Changes the OAuth2 client's logo
/// </summary>
[Route("/oauth2/clients/{Id}/logo", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class ChangeOAuth2ClientLogoRequest : UnTenantedRequest<ChangeOAuth2ClientLogoRequest, GetOAuth2ClientResponse>,
    IHasMultipartFormData
{
    // Will also include bytes for the multipart-form image
    [Required] public string? Id { get; set; }
}