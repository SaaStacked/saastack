using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.ApiHosts;

/// <summary>
///     Displays the health of the API
/// </summary>
[Route("/health", OperationMethod.Get)]
public class ApiHealthCheckRequest : UnTenantedRequest<ApiHealthCheckRequest, ApiHealthCheckResponse>;