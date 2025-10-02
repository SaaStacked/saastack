using AncillaryApplication;
using Common.FeatureFlags;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.FeatureFlags;

public class FeatureFlagsApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IFeatureFlagsApplication _featureFlagsApplication;

    public FeatureFlagsApi(ICallerContextFactory callerFactory, IFeatureFlagsApplication featureFlagsApplication)
    {
        _callerFactory = callerFactory;
        _featureFlagsApplication = featureFlagsApplication;
    }

    public async Task<ApiGetResult<FeatureFlag, GetFeatureFlagResponse>> Get(GetFeatureFlagRequest request,
        CancellationToken cancellationToken)
    {
        var flag = await _featureFlagsApplication.GetFeatureFlagAsync(_callerFactory.Create(),
            request.Name!, request.TenantId, request.UserId!, cancellationToken);

        return () => flag.HandleApplicationResult(f => new GetFeatureFlagResponse { Flag = f });
    }

    public async Task<ApiGetResult<List<FeatureFlag>, GetAllFeatureFlagsResponse>> GetAll(
        GetAllFeatureFlagsRequest request,
        CancellationToken cancellationToken)
    {
        var flags = await _featureFlagsApplication.GetAllFeatureFlagsAsync(_callerFactory.Create(), cancellationToken);

        return () => flags.HandleApplicationResult(f => new GetAllFeatureFlagsResponse { Flags = f });
    }

    public async Task<ApiGetResult<FeatureFlag, GetFeatureFlagResponse>> GetForCaller(
        GetFeatureFlagForCallerRequest request,
        CancellationToken cancellationToken)
    {
        var flag = await _featureFlagsApplication.GetFeatureFlagForCallerAsync(_callerFactory.Create(),
            request.Name!, cancellationToken);

        return () => flag.HandleApplicationResult(f => new GetFeatureFlagResponse { Flag = f });
    }
}