using System.ComponentModel.DataAnnotations;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Authorizes the user to access the application in Open ID Connect.
/// </summary>
[Route("/oauth2/authorize", OperationMethod.Post)]
[UsedImplicitly]
public class AuthorizeOAuth2Request : UnTenantedRequest<AuthorizeOAuth2Request, AuthorizeOAuth2Response>
{
    [Required] public string? ClientId { get; set; }

    public string? CodeChallenge { get; set; }

    public OpenIdConnectCodeChallengeMethod? CodeChallengeMethod { get; set; }

    public string? Nonce { get; set; }

    [Required] public string? RedirectUri { get; set; }

    [Required] public OAuth2ResponseType? ResponseType { get; set; }

    [Required] public string? Scope { get; set; }

    public string? State { get; set; }
}