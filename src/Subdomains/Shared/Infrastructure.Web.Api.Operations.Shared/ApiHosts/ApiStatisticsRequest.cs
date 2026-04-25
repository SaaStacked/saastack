using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.ApiHosts;

/// <summary>
///     Displays the statistics of the API
/// </summary>
[Route("/stats", OperationMethod.Get)]
public class ApiStatisticsRequest : UnTenantedRequest<ApiStatisticsRequest, ApiStatisticsResponse>
{
    public bool? Details { get; set; }
}