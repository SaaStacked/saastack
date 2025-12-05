using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Verifies that the password reset attempt is still valid
/// </summary>
/// <response code="404">The token is invalid or already used</response>
/// <response code="405">The password reset attempt has expired</response>
[Route("/credentials/{Token}/reset/verify", OperationMethod.Get)]
public class VerifyPasswordResetRequest : UnTenantedEmptyRequest<VerifyPasswordResetRequest>
{
    [Required] public string? Token { get; set; }
}