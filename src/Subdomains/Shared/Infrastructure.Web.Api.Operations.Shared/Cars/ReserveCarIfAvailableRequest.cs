using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

/// <summary>
///     Reserves the availability of the car for the specified period of time
/// </summary>
[Route("/cars/{Id}/reserve", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class ReserveCarIfAvailableRequest : TenantedRequest<ReserveCarIfAvailableRequest, ReserveCarIfAvailableResponse>
{
    [Required] public DateTime? FromUtc { get; set; }

    [Required] public string? Id { get; set; }

    [Required] public string? ReferenceId { get; set; }

    [Required] public DateTime? ToUtc { get; set; }
}