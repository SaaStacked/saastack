using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Confirms the registration of a new person (verifying their email address)
/// </summary>
/// <response code="400">The token is missing, or has expired and requires renewing</response>
/// <response code="404">The user has already confirmed their registration</response>
[Route("/credentials/confirm-registration", OperationMethod.Post)]
public class
    ConfirmPersonCredentialRegistrationRequest : UnTenantedEmptyRequest<ConfirmPersonCredentialRegistrationRequest>
{
    [Required] public string? Token { get; set; }
}