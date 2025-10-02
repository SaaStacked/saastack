using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

public class UploadImageResponse : IWebResponse
{
    public required Image Image { get; set; }
}