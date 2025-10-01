using System.Reflection;
using Common;
using Common.Configuration;
using Common.FeatureFlags;
using Infrastructure.External.TestingOnly.ApplicationServices;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.FakeProvider;

namespace TestingStubApiHost.Api;

/// <summary>
///     Represents an example of a testing stub that stands in for a fake feature flag provider,
///     that is used by the <see cref="FakeFeatureFlagProviderServiceClient" /> service client.
///     In local development and testing, appsettings configuration points the
///     <see cref="FakeFeatureFlagProviderServiceClient" />
///     service client to this stub (by URL).
///     In production builds, this host is not deployed, and appsettings points the service client to the real feature flag
///     provider over the internet.
///     This API mimics what the real fake provider does, with some pre-programmed responses.
/// </summary>
[BaseApiFrom("/fakefeatureflagprovider")]
public sealed class StubFakeFeatureFlagProviderApi : StubApiBase
{
    public StubFakeFeatureFlagProviderApi(IRecorder recorder, IConfigurationSettings settings) : base(recorder,
        settings)
    {
    }

    public async Task<ApiGetResult<string, ListFakeFeatureFlagProviderFlagsResponse>> GetAllFlags(
        ListFakeFeatureFlagProviderFlagsRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null, "FakeFeatureFlagProvider: GetAllFlags");

        var allKnownFlags = typeof(Flag).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Flag))
            .Select(f => (Flag)f.GetValue(null)!)
            .ToList();

        var counter = 1000;
        var flags = allKnownFlags
            .Select(f => new FakeFeatureFlagProviderFeatureFlag
            {
                Id = ++counter,
                IsEnabled = false,
                Name = f.Name
            }).ToList();

        return () =>
            new Result<ListFakeFeatureFlagProviderFlagsResponse, Error>(
                new ListFakeFeatureFlagProviderFlagsResponse
                {
                    Flags = flags
                });
    }

    public async Task<ApiGetResult<string, GetFakeFeatureFlagProviderFlagResponse>> GetFlag(
        GetFakeFeatureFlagProviderFlagRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null, "FakeFeatureFlagProvider: GetFlag for {Name}", request.Name ?? "none");
        return () => new GetFakeFeatureFlagProviderFlagResponse
        {
            Flag = new FakeFeatureFlagProviderFeatureFlag
            {
                Id = 1,
                IsEnabled = true,
                Name = request.Name ?? "none"
            }
        };
    }
}