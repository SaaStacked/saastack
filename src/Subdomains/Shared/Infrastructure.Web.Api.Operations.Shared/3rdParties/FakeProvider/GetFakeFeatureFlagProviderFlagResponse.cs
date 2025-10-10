#if TESTINGONLY
     using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.FakeProvider;

public class GetFakeFeatureFlagProviderFlagResponse : IWebResponse
{
    public FakeFeatureFlagProviderFeatureFlag? Flag { get; set; }
}

public class FakeFeatureFlagProviderFeatureFlag
{
    public bool IsEnabled { get; set; } = false;

    public required string Name { get; set; }

    public required int Id { get; set; }
}
#endif