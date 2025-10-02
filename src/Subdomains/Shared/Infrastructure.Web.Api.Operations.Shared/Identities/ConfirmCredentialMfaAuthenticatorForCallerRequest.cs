using System.ComponentModel.DataAnnotations;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Confirms the association of an MFA authenticator for the user, and authenticates the user.
/// </summary>
/// <response code="405">
///     The user has not yet associated an authenticator of the specified type
/// </response>
/// <remarks>
///     This API can be called Anonymously (during password authentication), as well as after being authenticated
/// </remarks>
[Route("/credentials/mfa/authenticators/{AuthenticatorType}/confirm", OperationMethod.PutPatch)]
public class ConfirmCredentialMfaAuthenticatorForCallerRequest : UnTenantedRequest<
    ConfirmCredentialMfaAuthenticatorForCallerRequest, ConfirmCredentialMfaAuthenticatorForCallerResponse>
{
    [Required] public CredentialMfaAuthenticatorType? AuthenticatorType { get; set; }

    [Required] public string? ConfirmationCode { get; set; }

    public string? MfaToken { get; set; }

    public string? OobCode { get; set; }
}