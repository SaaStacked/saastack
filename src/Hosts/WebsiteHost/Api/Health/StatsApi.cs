using Application.Resources.Shared;
using Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.ApiHosts;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using Swashbuckle.AspNetCore.Swagger;

namespace WebsiteHost.Api.Health;

[BaseApiFrom("/api")]
public sealed class StatsApi : IWebApiService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISwaggerProvider _swaggerProvider;
    private readonly WebHostOptions _webHostOptions;

    public StatsApi(IHttpContextAccessor httpContextAccessor, WebHostOptions webHostOptions,
        ISwaggerProvider swaggerProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _webHostOptions = webHostOptions;
        _swaggerProvider = swaggerProvider;
    }

    public async Task<ApiResult<ApiStatistics, ApiStatisticsResponse>> Stats(ApiStatisticsRequest request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        var stats = await httpContext.BuildApiStatisticsAsync(_webHostOptions, _swaggerProvider,
            request.Details ?? false,
            cancellationToken);

        return () => new Result<ApiStatisticsResponse, Error>(new ApiStatisticsResponse
        {
            Statistics = stats
        });
    }
}