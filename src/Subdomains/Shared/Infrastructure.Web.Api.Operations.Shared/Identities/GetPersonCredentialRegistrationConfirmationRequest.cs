#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches the confirmation token for a registration of a person
/// </summary>
[Route("/credentials/confirm-registration", OperationMethod.Get, isTestingOnly: true)]
public class GetPersonCredentialRegistrationConfirmationRequest : UnTenantedRequest<
    GetPersonCredentialRegistrationConfirmationRequest,
    GetPersonCredentialRegistrationConfirmationResponse>
{
    [Required] public string? UserId { get; set; }
}
#endif