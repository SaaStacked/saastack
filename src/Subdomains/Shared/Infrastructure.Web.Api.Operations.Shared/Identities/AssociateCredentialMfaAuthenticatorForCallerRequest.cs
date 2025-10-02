using System.ComponentModel.DataAnnotations;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Associates another MFA authentication factor to the user.
///     Depending on the specific authenticator, this can send an SMS or email to the user containing a secret code.
/// </summary>
/// <response code="405">
///     The user has already associated at least one other authenticator, or this authenticator is already associated.
///     You must make a challenge using an existing association
/// </response>
/// <remarks>
///     This API can be called Anonymously (during password authentication), as well as after being authenticated
/// </remarks>
[Route("/credentials/mfa/authenticators", OperationMethod.Post)]
public class AssociateCredentialMfaAuthenticatorForCallerRequest : UnTenantedRequest<
    AssociateCredentialMfaAuthenticatorForCallerRequest, AssociateCredentialMfaAuthenticatorForCallerResponse>
{
    [Required] public CredentialMfaAuthenticatorType? AuthenticatorType { get; set; }

    public string? MfaToken { get; set; }

    public string? PhoneNumber { get; set; }
}