using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.FeatureFlags;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace WebsiteHost.Application;

public class FeatureFlagsApplication : IFeatureFlagsApplication
{
    private readonly string _hmacSecret;
    private readonly IServiceClient _serviceClient;

    public FeatureFlagsApplication(IServiceClient serviceClient, IHostSettings hostSettings)
    {
        _serviceClient = serviceClient;
        _hmacSecret = hostSettings.GetAncillaryApiHostHmacAuthSecret();
    }

    public async Task<Result<List<FeatureFlag>, Error>> GetAllFeatureFlagsAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var request = new GetAllFeatureFlagsRequest();

        var retrieved = await _serviceClient.GetAsync(caller, request, req =>
            {
                req.RemoveAuthorization();
                req.SetHMACAuth(request, _hmacSecret);
            }, null,
            cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error.ToError();
        }

        return retrieved.Value.Flags;
    }

    public async Task<Result<FeatureFlag, Error>> GetFeatureFlagForCallerAsync(ICallerContext caller, string name,
        CancellationToken cancellationToken)
    {
        var request = new GetFeatureFlagForCallerRequest
        {
            Name = name
        };

        var retrieved = await _serviceClient.GetAsync(caller, request, cancellationToken: cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error.ToError();
        }

        return retrieved.Value.Flag;
    }
}