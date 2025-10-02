using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

/// <summary>
///     Downloads the raw image
/// </summary>
[Route("/images/{Id}/download", OperationMethod.Get)]
public class DownloadImageRequest : UnTenantedStreamRequest<DownloadImageRequest>
{
    [Required] public string? Id { get; set; }
}