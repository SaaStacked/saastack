using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.ApiHosts;

public class ApiHealthCheckResponse : IWebResponse
{
    public required ApiHostHealth Health { get; set; }
}