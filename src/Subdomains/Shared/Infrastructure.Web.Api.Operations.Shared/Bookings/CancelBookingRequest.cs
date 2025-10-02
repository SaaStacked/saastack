using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Bookings;

/// <summary>
///     Cancels a booking
/// </summary>
[Route("/bookings/{Id}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class CancelBookingRequest : TenantedDeleteRequest<CancelBookingRequest>
{
    [Required] public string? Id { get; set; }
}