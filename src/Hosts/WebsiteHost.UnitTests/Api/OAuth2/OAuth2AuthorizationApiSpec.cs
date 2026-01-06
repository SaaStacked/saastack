using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using Microsoft.AspNetCore.Http;
using Moq;
using UnitTesting.Common;
using WebsiteHost.Api.OAuth2;
using WebsiteHost.Application;
using Xunit;

namespace WebsiteHost.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class OAuth2AuthorizationApiSpec
{
    private readonly OAuth2AuthorizationApi _api;
    private readonly Mock<IOAuth2AuthorizationApplication> _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IRequestCookieCollection> _httpRequestCookies;
    private readonly Mock<IResponseCookies> _httpResponseCookies;

    public OAuth2AuthorizationApiSpec()
    {
        _application = new Mock<IOAuth2AuthorizationApplication>();
        _caller = new Mock<ICallerContext>();
        var callerFactory = new Mock<ICallerContextFactory>();
        callerFactory.Setup(ccf => ccf.Create())
            .Returns(_caller.Object);
        var httpRequest = new Mock<HttpRequest>();
        _httpRequestCookies = new Mock<IRequestCookieCollection>();
        httpRequest.Setup(req => req.Cookies).Returns(_httpRequestCookies.Object);
        httpRequest.Setup(req => req.Host).Returns(new HostString("localhost"));
        var httpResponse = new Mock<HttpResponse>();
        _httpResponseCookies = new Mock<IResponseCookies>();
        httpResponse.Setup(res => res.Cookies).Returns(_httpResponseCookies.Object);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hca => hca.HttpContext!.Request)
            .Returns(httpRequest.Object);
        httpContextAccessor.Setup(hca => hca.HttpContext!.Response)
            .Returns(httpResponse.Object);
        _api = new OAuth2AuthorizationApi(callerFactory.Object, _application.Object, httpContextAccessor.Object);
    }

    [Fact]
    public async Task WhenAuthorizeAndAuthorizationFails_ThenReturnsError()
    {
        _httpRequestCookies.Setup(c =>
                c.TryGetValue(AuthenticationConstants.Cookies.PendingOAuth2Authorization, out It.Ref<string?>.IsAny))
            .Returns((string _, ref string? value) =>
            {
                value = null;
                return false;
            });
        _application.Setup(app => app.AuthorizeAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<OAuth2ResponseType?>(),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<OpenIdConnectCodeChallengeMethod?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("anerror"));

        var result = await _api.Authorize(new AuthorizeOAuth2Request
        {
            ClientId = "aclientid",
            RedirectUri = "aredirecturi",
            ResponseType = OAuth2ResponseType.Code,
            Scope = "ascope",
            State = "astate",
            Nonce = "anonce",
            CodeChallenge = "acodechallenge",
            CodeChallengeMethod = OpenIdConnectCodeChallengeMethod.Plain
        }, CancellationToken.None);

        result.Invoke().Should().BeError(ErrorCode.Unexpected, "anerror");
        _application.Verify(app => app.AuthorizeAsync(_caller.Object, "aclientid", "aredirecturi",
            OAuth2ResponseType.Code, "ascope", "astate", "anonce", OpenIdConnectCodeChallengeMethod.Plain,
            "acodechallenge", It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(rc =>
            rc.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()), Times.Never);
        _httpResponseCookies.Verify(rc => rc.Delete(AuthenticationConstants.Cookies.PendingOAuth2Authorization));
    }

    [Fact]
    public async Task WhenAuthorizeWithEmptyRequestAndPendingAuthorization_ThenResumesAuthorization()
    {
        _httpRequestCookies.Setup(c =>
                c.TryGetValue(AuthenticationConstants.Cookies.PendingOAuth2Authorization, out It.Ref<string?>.IsAny))
            .Returns((string _, ref string? value) =>
            {
                value = new AuthorizeOAuth2Request
                {
                    ClientId = "aclientid",
                    RedirectUri = "aredirecturi",
                    ResponseType = OAuth2ResponseType.Code,
                    Scope = "ascope",
                    State = "astate",
                    Nonce = "anonce",
                    CodeChallenge = "acodechallenge",
                    CodeChallengeMethod = OpenIdConnectCodeChallengeMethod.Plain
                }.ToJson(false)!;
                return true;
            });
        const string location = "https://localhost:5101/alocation";
        _application.Setup(app => app.AuthorizeAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<OAuth2ResponseType?>(),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<OpenIdConnectCodeChallengeMethod?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var result = await _api.Authorize(new AuthorizeOAuth2Request(), CancellationToken.None);

        var redirect = result.Invoke().Value.Response.Redirect;
        redirect.IsLogin.Should().BeFalse();
        redirect.IsConsent.Should().BeFalse();
        redirect.IsExternal.Should().BeFalse();
        redirect.RedirectUri.Should().Be(location);
        _application.Verify(app => app.AuthorizeAsync(_caller.Object, "aclientid", "aredirecturi",
            OAuth2ResponseType.Code, "ascope", "astate", "anonce", OpenIdConnectCodeChallengeMethod.Plain,
            "acodechallenge", It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(rc =>
            rc.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()), Times.Never);
        _httpResponseCookies.Verify(rc => rc.Delete(AuthenticationConstants.Cookies.PendingOAuth2Authorization));
    }

    [Fact]
    public async Task WhenAuthorizeAndAuthorizationRedirectsToExternalRedirectUri_ThenReturnsRedirect()
    {
        _httpRequestCookies.Setup(c =>
                c.TryGetValue(AuthenticationConstants.Cookies.PendingOAuth2Authorization, out It.Ref<string?>.IsAny))
            .Returns((string _, ref string? value) =>
            {
                value = null;
                return false;
            });
        const string location = "https://otherserver:5101/alocation";
        _application.Setup(app => app.AuthorizeAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<OAuth2ResponseType?>(),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<OpenIdConnectCodeChallengeMethod?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var result = await _api.Authorize(new AuthorizeOAuth2Request
        {
            ClientId = "aclientid",
            RedirectUri = "aredirecturi",
            ResponseType = OAuth2ResponseType.Code,
            Scope = "ascope",
            State = "astate",
            Nonce = "anonce",
            CodeChallenge = "acodechallenge",
            CodeChallengeMethod = OpenIdConnectCodeChallengeMethod.Plain
        }, CancellationToken.None);

        var redirect = result.Invoke().Value.Response.Redirect;
        redirect.IsLogin.Should().BeFalse();
        redirect.IsConsent.Should().BeFalse();
        redirect.IsExternal.Should().BeTrue();
        redirect.RedirectUri.Should().Be(location);
        _application.Verify(app => app.AuthorizeAsync(_caller.Object, "aclientid", "aredirecturi",
            OAuth2ResponseType.Code, "ascope", "astate", "anonce", OpenIdConnectCodeChallengeMethod.Plain,
            "acodechallenge", It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(rc =>
            rc.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()), Times.Never);
        _httpResponseCookies.Verify(rc => rc.Delete(AuthenticationConstants.Cookies.PendingOAuth2Authorization));
    }

    [Fact]
    public async Task
        WhenAuthorizeAndAuthorizationRedirectsToLoginPage_ThenSavesPendingAuthorizationAndReturnsRedirect()
    {
        _httpRequestCookies.Setup(c =>
                c.TryGetValue(AuthenticationConstants.Cookies.PendingOAuth2Authorization, out It.Ref<string?>.IsAny))
            .Returns((string _, ref string? value) =>
            {
                value = null;
                return false;
            });
        const string location = $"http://localhost/{WebsiteUiService.LoginPageRoute}";
        _application.Setup(app => app.AuthorizeAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<OAuth2ResponseType?>(),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<OpenIdConnectCodeChallengeMethod?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var result = await _api.Authorize(new AuthorizeOAuth2Request
        {
            ClientId = "aclientid",
            RedirectUri = "aredirecturi",
            ResponseType = OAuth2ResponseType.Code,
            Scope = "ascope",
            State = "astate",
            Nonce = "anonce",
            CodeChallenge = "acodechallenge",
            CodeChallengeMethod = OpenIdConnectCodeChallengeMethod.Plain
        }, CancellationToken.None);

        var redirect = result.Invoke().Value.Response.Redirect;
        redirect.IsLogin.Should().BeTrue();
        redirect.IsConsent.Should().BeFalse();
        redirect.IsExternal.Should().BeFalse();
        redirect.RedirectUri.Should().Be(location);
        _application.Verify(app => app.AuthorizeAsync(_caller.Object, "aclientid", "aredirecturi",
            OAuth2ResponseType.Code, "ascope", "astate", "anonce", OpenIdConnectCodeChallengeMethod.Plain,
            "acodechallenge", It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.PendingOAuth2Authorization, It.Is<string>(s =>
                s.FromJson<AuthorizeOAuth2Request>()!.ClientId == "aclientid"
            ), It.Is<CookieOptions>(opt =>
                opt.Expires!.Value.DateTime.IsNear(
                    DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultPendingOAuth2AuthorizationExpiry))
            )));
        _httpResponseCookies.Verify(rc => rc.Delete(AuthenticationConstants.Cookies.PendingOAuth2Authorization),
            Times.Never);
    }
}