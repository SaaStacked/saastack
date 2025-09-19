using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Renews and resends a confirmation email for confirming the registration of a person
/// </summary>
/// <response code="400">The token is missing</response>
/// <response code="404">The user has already confirmed their registration</response>
[Route("/credentials/resend-confirmation", OperationMethod.Post)]
public class
    ResendPersonCredentialRegistrationConfirmationRequest : UnTenantedEmptyRequest<
    ResendPersonCredentialRegistrationConfirmationRequest>
{
    [Required] public string? Token { get; set; }
}