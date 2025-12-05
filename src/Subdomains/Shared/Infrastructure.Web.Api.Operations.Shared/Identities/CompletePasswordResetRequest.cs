using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Completes a password reset attempt
/// </summary>
/// <response code="400">The password is not valid</response>
/// <response code="404">The token is invalid or already used</response>
/// <response code="405">The user is not yet registered, or the password reset attempt has expired</response>
/// <response code="409">The password is the same as the last password</response>
[Route("/credentials/{Token}/reset/complete", OperationMethod.Post)]
public class CompletePasswordResetRequest : UnTenantedEmptyRequest<CompletePasswordResetRequest>
{
    [Required] public string? Password { get; set; }

    [Required] public string? Token { get; set; }
}