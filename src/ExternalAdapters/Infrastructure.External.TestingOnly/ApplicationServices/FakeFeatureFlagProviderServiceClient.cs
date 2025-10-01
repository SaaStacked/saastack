using System.Text.Json;
using Common;
using Common.Configuration;
using Common.FeatureFlags;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.FakeProvider;

namespace Infrastructure.External.TestingOnly.ApplicationServices;

/// <summary>
///     Represents an example service client adapter to a 3rd party feature flag provider
///     The fake provider API that this adapter calls will always be a stub, and will always return true for all flags.
///     You will find the stubbed API in the TestingStubApiHost project.
/// </summary>
public class FakeFeatureFlagProviderServiceClient : IFeatureFlags
{
    private const bool DefaultEnabled = true;
    private const string FakeFeatureFlagProviderBaseUrlSettingName =
        "ApplicationServices:FakeFeatureFlagProvider:BaseUrl";
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;

    public FakeFeatureFlagProviderServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory clientFactory, JsonSerializerOptions jsonSerializerOptions)
        : this(recorder, new ApiServiceClient(clientFactory, jsonSerializerOptions,
            settings.Platform.GetString(FakeFeatureFlagProviderBaseUrlSettingName)))
    {
    }

    internal FakeFeatureFlagProviderServiceClient(IRecorder recorder, IServiceClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    public async Task<Result<IReadOnlyList<FeatureFlag>, Error>> GetAllFlagsAsync(CancellationToken cancellationToken)
    {
        var response = await _serviceClient.GetAsync(null, new ListFakeFeatureFlagProviderFlagsRequest(), null,
            cancellationToken);
        if (response.IsFailure)
        {
            return response.Error.ToError();
        }

        var featureFlags = response.Value.Flags;
        _recorder.TraceInformation(null, "FakeFeatureFlagProvider: GetAllFlags");
        return featureFlags.Select(f => new FeatureFlag
            {
                Name = f.Name,
                IsEnabled = f.IsEnabled
            })
            .ToList();
    }

    public async Task<Result<FeatureFlag, Error>> GetFlagAsync(Flag flag, Optional<string> tenantId,
        Optional<string> userId, CancellationToken cancellationToken)
    {
        var response = await _serviceClient.GetAsync(null, new GetFakeFeatureFlagProviderFlagRequest
            {
                Name = flag.Name
            }, null,
            cancellationToken);
        if (response.IsFailure)
        {
            return response.Error.ToError();
        }

        var featureFlag = response.Value.Flag!;
        _recorder.TraceInformation(null, "FakeFeatureFlagProvider: GetFlag {Name}, as {IsEnabled}", featureFlag.Name,
            featureFlag.IsEnabled);
        return new FeatureFlag
        {
            Name = featureFlag.Name,
            IsEnabled = featureFlag.IsEnabled
        };
    }

    public bool IsEnabled(Flag flag)
    {
        _recorder.TraceInformation(null, "FakeFeatureFlagProvider: IsEnabled {Name}, as {IsEnabled}", flag.Name,
            DefaultEnabled);
        return DefaultEnabled;
    }

    public bool IsEnabled(Flag flag, string userId)
    {
        _recorder.TraceInformation(null, "FakeFeatureFlagProvider: IsEnabled {Name}, for user {UserId}, as {IsEnabled}",
            flag.Name, userId, DefaultEnabled);
        return DefaultEnabled;
    }

    public bool IsEnabled(Flag flag, Optional<string> tenantId, string userId)
    {
        _recorder.TraceInformation(null,
            "FakeFeatureFlagProvider: IsEnabled {Name}, for user {UserId}, of tenant {TenantId}, as {IsEnabled}",
            flag.Name, userId, tenantId, DefaultEnabled);
        return DefaultEnabled;
    }
}