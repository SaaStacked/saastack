#if TESTINGONLY
     using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.FakeProvider;

public class ListFakeFeatureFlagProviderFlagsResponse : IWebResponse
{
    public List<FakeFeatureFlagProviderFeatureFlag> Flags { get; set; } = [];
}
#endif