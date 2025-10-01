using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.FakeProvider;

/// <summary>
///     Fetches all features flags from the fake feature flag provider
/// </summary>
[Route("/flags", OperationMethod.Get)]
public class ListFakeFeatureFlagProviderFlagsRequest : UnTenantedRequest<ListFakeFeatureFlagProviderFlagsRequest,
    ListFakeFeatureFlagProviderFlagsResponse>
{
}