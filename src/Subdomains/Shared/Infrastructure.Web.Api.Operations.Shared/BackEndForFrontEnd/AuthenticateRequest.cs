using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

/// <summary>
///     Authenticates the user with the specified provider, using either an auth code or a username and password.
/// </summary>
/// <response code="401">The user's credentials are invalid</response>
/// <response code="405">When the user has authenticated with credentials, but has not yet verified their registration</response>
/// <response code="403">
///     When the user has authenticated with credentials, but has MFA enabled. The details of the error
///     contains a value of "mfa_required".
/// </response>
/// <response code="423">The user's account is suspended or disabled, and cannot be authenticated or used</response>
[Route("/auth", OperationMethod.Post)]
public class AuthenticateRequest : UnTenantedRequest<AuthenticateRequest, AuthenticateResponse>
{
    public string? AuthCode { get; set; }

    public string? Password { get; set; }

    [Required] public string? Provider { get; set; }

    public string? Username { get; set; }
}