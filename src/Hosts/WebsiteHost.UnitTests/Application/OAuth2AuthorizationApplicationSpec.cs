using System.Net;
using Application.Interfaces;
using Application.Resources.Shared;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Moq;
using UnitTesting.Common;
using WebsiteHost.Application;
using Xunit;

namespace WebsiteHost.UnitTests.Application;

[Trait("Category", "Unit")]
public class OAuth2AuthorizationApplicationSpec
{
    private readonly OAuth2AuthorizationApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IServiceClient> _serviceClient;

    public OAuth2AuthorizationApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _serviceClient = new Mock<IServiceClient>();
        _application = new OAuth2AuthorizationApplication(_serviceClient.Object);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndClientRedirects_ThenReturnsRedirect()
    {
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<AuthorizeOAuth2Request>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<Action<HttpResponseMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ICallerContext _, AuthorizeOAuth2Request _, Action<HttpRequestMessage> _,
                Action<HttpResponseMessage> responseInterceptor, CancellationToken _) =>
            {
                responseInterceptor(new HttpResponseMessage(HttpStatusCode.Redirect)
                {
                    Headers = { Location = new Uri("https://locahost/alocation") }
                });

                return new EmptyResponse();
            });

        var result = await _application.AuthorizeAsync(_caller.Object, "aclientid", "aredirecturi",
            OAuth2ResponseType.Code, "ascope", "astate", "anonce", OpenIdConnectCodeChallengeMethod.Plain,
            "acodechallenge", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().Be("https://locahost/alocation");
    }

    [Fact]
    public async Task WhenConsentToClientAsyncAndClientRedirects_ThenReturnsRedirectResult()
    {
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(),
                It.IsAny<ConsentOAuth2ClientForCallerRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<Action<HttpResponseMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ICallerContext _, ConsentOAuth2ClientForCallerRequest _, Action<HttpRequestMessage> _,
                Action<HttpResponseMessage> responseInterceptor, CancellationToken _) =>
            {
                responseInterceptor(new HttpResponseMessage(HttpStatusCode.Redirect)
                {
                    Headers = { Location = new Uri("https://locahost/alocation") }
                });

                return new GetOAuth2ClientConsentResponse();
            });

        var result = await _application.ConsentToClientAsync(_caller.Object, "aclientid", "aredirecturi",
            "ascope", true, "astate", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Consent.Should().BeNull();
        result.Value.DenyErrorRedirectUri.Should().Be("https://locahost/alocation");
    }

    [Fact]
    public async Task WhenConsentToClientAsyncAndClientReturnsResponse_ThenReturnsResponseResult()
    {
        var consent = new OAuth2ClientConsent
        {
            ClientId = "aclientid",
            IsConsented = true,
            Scopes = [],
            UserId = "auserid",
            Id = "aid"
        };
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(),
                It.IsAny<ConsentOAuth2ClientForCallerRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<Action<HttpResponseMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ICallerContext _, ConsentOAuth2ClientForCallerRequest _, Action<HttpRequestMessage> _,
                Action<HttpResponseMessage> responseInterceptor, CancellationToken _) =>
            {
                responseInterceptor(new HttpResponseMessage(HttpStatusCode.OK));

                return new GetOAuth2ClientConsentResponse
                {
                    Consent = consent
                };
            });

        var result = await _application.ConsentToClientAsync(_caller.Object, "aclientid", "aredirecturi",
            "ascope", true, "astate", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Consent.Should().Be(consent);
        result.Value.DenyErrorRedirectUri.Should().BeNull();
    }
}