using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Begins a password reset attempt
/// </summary>
/// <response code="405">The user is not yet registered</response>
[Route("/credentials/reset", OperationMethod.Post)]
public class InitiatePasswordResetRequest : UnTenantedEmptyRequest<InitiatePasswordResetRequest>
{
    [Required] public string? EmailAddress { get; set; }
}