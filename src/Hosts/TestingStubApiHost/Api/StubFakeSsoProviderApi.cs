#if TESTINGONLY
using Common;
using Common.Configuration;
using Domain.Interfaces;
using Infrastructure.External.TestingOnly.ApplicationServices;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;

namespace TestingStubApiHost.Api;

/// <summary>
///     Represents an example of a testing stub that stands in for a fake SSO provider,
///     that is used by the WebSiteHost Javascript App.
///     In production builds, this host is not deployed.
///     This API mimics what the real fake provider does, with some pre-programmed responses.
/// </summary>
[BaseApiFrom("/fakessoprovider")]
public class StubFakeSsoProviderApi : StubApiBase
{
    public StubFakeSsoProviderApi(IRecorder recorder, IConfigurationSettings settings) : base(recorder, settings)
    {
    }

    public async Task<ApiRedirectResult<string, EmptyResponse>> Authorize(
        GenericOAuth2AuthorizeRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubFakeSsoProviderApi: Authorize grant {Type}, for scope {Scope}, for client {ClientId}, and redirect to {RedirectUri}, with PKCE: challenge: {CodeChallenge} ({CodeChallengeMethod}), and state: {State}",
            request.ResponseType ?? "none", request.Scope ?? "none", request.ClientId ?? "none",
            request.RedirectUri ?? "none", request.CodeChallenge ?? "none", request.CodeChallengeMethod ?? "none",
            request.State ?? "none");

        if (request.ResponseType == OAuth2Constants.ResponseTypes.Code)
        {
            var redirectUri = $"{request.RedirectUri}?code={FakeOAuth2Service.AuthCode1}&state={request.State}";
            return () => new RedirectResult<EmptyResponse>(
                new EmptyResponse(), redirectUri);
        }

        return () => Error.NotAuthenticated();
    }
}
#endif