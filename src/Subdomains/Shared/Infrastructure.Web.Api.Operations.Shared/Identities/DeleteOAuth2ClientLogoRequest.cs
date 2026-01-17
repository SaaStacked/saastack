using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Removes the OAuth2 client's logo
/// </summary>
[Route("/oauth2/clients/{Id}/logo", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class DeleteOAuth2ClientLogoRequest : UnTenantedRequest<DeleteOAuth2ClientLogoRequest, GetOAuth2ClientResponse>
{
    [Required] public string? Id { get; set; }
}