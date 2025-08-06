using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Moq;
using UnitTesting.Common;
using WebsiteHost.Application;
using Xunit;

namespace WebsiteHost.UnitTests.Application;

[Trait("Category", "Unit")]
public class AuthenticationApplicationSpec
{
    private readonly AuthenticationApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IServiceClient> _serviceClient;

    public AuthenticationApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _serviceClient = new Mock<IServiceClient>();

        _application =
            new AuthenticationApplication(_recorder.Object, _serviceClient.Object);
    }

    [Fact]
    public async Task WhenLogout_ThenLogsOut()
    {
        var result = await _application.LogoutAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.UsageScenarios.Generic.UserLogout, null));
    }

    [Fact]
    public async Task WhenAuthenticateWithCredentials_ThenAuthenticates()
    {
        var accessTokenExpiresOn = DateTime.UtcNow;
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<AuthenticateCredentialRequest>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateResponse
            {
                Tokens = new AuthenticateTokens
                {
                    UserId = "auserid",
                    AccessToken = new AuthenticationToken
                    {
                        Value = "anaccesstoken",
                        ExpiresOn = accessTokenExpiresOn,
                        Type = TokenType.AccessToken
                    },
                    RefreshToken = new AuthenticationToken
                    {
                        Value = "arefreshtoken",
                        ExpiresOn = refreshTokenExpiresOn,
                        Type = TokenType.RefreshToken
                    }
                }
            });

        var result = await _application.AuthenticateAsync(_caller.Object, AuthenticationConstants.Providers.Credentials,
            null, "ausername", "apassword", CancellationToken.None);

        result.Value.UserId.Should().Be("auserid");
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(accessTokenExpiresOn);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.RefreshToken.ExpiresOn.Should().Be(refreshTokenExpiresOn);
        _serviceClient.Verify(sc => sc.PostAsync(_caller.Object, It.Is<AuthenticateCredentialRequest>(req =>
            req.Username == "ausername"
            && req.Password == "apassword"
        ), null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAuthenticateWithSingleSignOn_ThenAuthenticates()
    {
        var accessTokenExpiresOn = DateTime.UtcNow;
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<AuthenticateSingleSignOnRequest>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateResponse
            {
                Tokens = new AuthenticateTokens
                {
                    UserId = "auserid",
                    AccessToken = new AuthenticationToken
                    {
                        Value = "anaccesstoken",
                        ExpiresOn = accessTokenExpiresOn,
                        Type = TokenType.AccessToken
                    },
                    RefreshToken = new AuthenticationToken
                    {
                        Value = "arefreshtoken",
                        ExpiresOn = refreshTokenExpiresOn,
                        Type = TokenType.RefreshToken
                    }
                }
            });

        var result = await _application.AuthenticateAsync(_caller.Object, "aprovider", "anauthcode", null, null,
            CancellationToken.None);

        result.Value.UserId.Should().Be("auserid");
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(accessTokenExpiresOn);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.RefreshToken.ExpiresOn.Should().Be(refreshTokenExpiresOn);
        _serviceClient.Verify(sc => sc.PostAsync(_caller.Object, It.Is<AuthenticateSingleSignOnRequest>(req =>
            req.AuthCode == "anauthcode"
        ), null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenAndCookieNotExist_ThenReturnsError()
    {
        var result = await _application.RefreshTokenAsync(_caller.Object, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _serviceClient.Verify(
            sc => sc.PostAsync(_caller.Object, It.IsAny<RefreshTokenRequest>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), It.IsAny<string>(), null), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenCookieExists_ThenRefreshesAndSetsCookie()
    {
        var accessTokenExpiresOn = DateTime.UtcNow;
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<RefreshTokenRequest>(),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResponse
            {
                Tokens = new AuthenticateTokens
                {
                    UserId = "auserid",
                    AccessToken = new AuthenticationToken
                    {
                        Value = "anaccesstoken",
                        ExpiresOn = accessTokenExpiresOn,
                        Type = TokenType.AccessToken
                    },
                    RefreshToken = new AuthenticationToken
                    {
                        Value = "arefreshtoken",
                        ExpiresOn = refreshTokenExpiresOn,
                        Type = TokenType.RefreshToken
                    }
                }
            });

        var result = await _application.RefreshTokenAsync(_caller.Object, "arefreshtoken", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.UserId.Should().Be("auserid");
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(accessTokenExpiresOn);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.RefreshToken.ExpiresOn.Should().Be(refreshTokenExpiresOn);
        _serviceClient.Verify(
            sc => sc.PostAsync(_caller.Object, It.Is<RefreshTokenRequest>(req =>
                req.RefreshToken == "arefreshtoken"
            ), null, It.IsAny<CancellationToken>()));
    }
}