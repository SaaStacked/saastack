#if TESTINGONLY
     using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.FakeProvider;

/// <summary>
///     Fetches a feature flag from the fake feature flag provider
/// </summary>
[Route("/flags/{Name}", OperationMethod.Get)]
public class GetFakeFeatureFlagProviderFlagRequest : UnTenantedRequest<GetFakeFeatureFlagProviderFlagRequest,
    GetFakeFeatureFlagProviderFlagResponse>
{
    public string? Name { get; set; }
}
#endif