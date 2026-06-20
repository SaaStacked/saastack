using System.Reflection;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.ApiHosts;
using Infrastructure.Web.Hosting.Common;

namespace WebsiteHost.Api.Health;

[BaseApiFrom("/api")]
public sealed class HealthApi : IWebApiService
{
    private readonly WebHostOptions _webHostOptions;

    public HealthApi(WebHostOptions webHostOptions)
    {
        _webHostOptions = webHostOptions;
    }

    public async Task<ApiResult<string, ApiHealthCheckResponse>> Check(ApiHealthCheckRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetHostVersions();
        return () => new Result<ApiHealthCheckResponse, Error>(new ApiHealthCheckResponse
        {
            Health = new ApiHostHealth
            {
                Name = _webHostOptions.HostName,
                Status = "OK",
                Version = version
            }
        });
    }
}