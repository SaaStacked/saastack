using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Resends a password reset attempt (via email)
/// </summary>
/// <response code="404">The token is invalid or already used</response>
/// <response code="405">The user is not yet registered</response>
[Route("/credentials/{Token}/reset/resend", OperationMethod.Post)]
public class ResendPasswordResetRequest : UnTenantedEmptyRequest<ResendPasswordResetRequest>
{
    [Required] public string? Token { get; set; }
}