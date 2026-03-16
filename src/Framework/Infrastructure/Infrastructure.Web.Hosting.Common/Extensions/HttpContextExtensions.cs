using Application.Resources.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    ///     Builds the statistics for the API, based on the Open API document.
    /// </summary>
    public static async Task<ApiStatistics> BuildApiStatisticsAsync(this HttpContext context,
        WebHostOptions webHostOptions, ISwaggerProvider swaggerProvider, bool includeDetails,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var baseUrl = context.Request.Scheme + "://" + context.Request.Host;
        var document = swaggerProvider.GetSwagger(webHostOptions.HostApiVersion);
        return BuildStatistics(document, baseUrl, includeDetails);
    }

    private static ApiStatistics BuildStatistics(OpenApiDocument document, string baseUrl, bool includeDetails)
    {
        var methodGroups = new Dictionary<OperationType, List<EndpointStatistic>>();
        foreach (var (fullPath, pathItem) in document.Paths)
        {
            foreach (var (operationType, operation) in pathItem.Operations)
            {
                if (!methodGroups.TryGetValue(operationType, out var endpoints))
                {
                    endpoints = [];
                    methodGroups[operationType] = endpoints;
                }

                endpoints.Add(new EndpointStatistic
                {
                    Path = fullPath,
                    Description = operation.Summary ?? operation.Description ?? string.Empty,
                    Tags = operation.Tags.Select(t => t.Name).ToList()
                });
            }
        }

        var gets = ToMethods(methodGroups, OperationType.Get, includeDetails);
        var posts = ToMethods(methodGroups, OperationType.Post, includeDetails);
        var puts = ToMethods(methodGroups, OperationType.Put, includeDetails);
        var patches = ToMethods(methodGroups, OperationType.Patch, includeDetails);
        var deletes = ToMethods(methodGroups, OperationType.Delete, includeDetails);

        var totalMethods = gets.Total + posts.Total + puts.Total + patches.Total + deletes.Total;
        return new ApiStatistics
        {
            Name = document.Info.Title,
            BaseUrl = baseUrl,
            ApiVersion = document.Info.Version,
            Methods = new MethodGroupStatistics
            {
                Gets = gets,
                Posts = posts,
                Puts = puts,
                Patches = patches,
                Deletes = deletes,
                Total = totalMethods
            },
            Total = totalMethods
        };
    }

    private static MethodGroupEndpointStatistics ToMethods(
        Dictionary<OperationType, List<EndpointStatistic>> endpointsByMethod,
        OperationType method, bool includeDetails)
    {
        var endpoints = endpointsByMethod.GetValueOrDefault(method, []);

        return new MethodGroupEndpointStatistics
        {
            Total = endpoints.Count,
            Endpoints = includeDetails
                ? endpoints.OrderBy(e => e.Path).ToList()
                : null
        };
    }
}