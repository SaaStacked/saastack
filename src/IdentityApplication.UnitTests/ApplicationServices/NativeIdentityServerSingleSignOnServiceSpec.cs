using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.ValueObjects;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class NativeIdentityServerSingleSignOnServiceSpec
{
    private readonly Mock<IAuthTokensService> _authTokensService;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly NativeIdentityServerSingleSignOnService _service;
    private readonly Mock<ISSOAuthenticationProvider> _ssoProvider;
    private readonly Mock<ISSOProvidersService> _ssoProvidersService;

    public NativeIdentityServerSingleSignOnServiceSpec()
    {
        var recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _endUsersService = new Mock<IEndUsersService>();
        _ssoProvider = new Mock<ISSOAuthenticationProvider>();
        _ssoProvidersService = new Mock<ISSOProvidersService>();
        _ssoProvidersService.Setup(sps =>
                sps.FindProviderByUserIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ssoProvider.Object.ToOptional());
        _authTokensService = new Mock<IAuthTokensService>();

        _service = new NativeIdentityServerSingleSignOnService(recorder.Object, _endUsersService.Object,
            _ssoProvidersService.Object, _authTokensService.Object);
    }

    [Fact]
    public async Task WhenAuthenticateAndProviderReturnsAuthenticationError_ThenReturnsError()
    {
        _ssoProvidersService.Setup(sps =>
                sps.AuthenticateUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotAuthenticated("amessage"));

        var result = await _service.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null, null, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _ssoProvidersService.Verify(sps =>
            sps.AuthenticateUserAsync(_caller.Object, "aprovidername", "anauthcode", null, null,
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAuthenticateAndPersonExistsButNotRegisteredYet_ThenIssuesToken()
    {
        var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "auser@company.com", "afirstname", null,
            Timezones.Default,
            CountryCodes.Default);
        _ssoProvidersService.Setup(sps =>
                sps.AuthenticateUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(authUserInfo);
        var ssoUser = new SSOUser
        {
            Id = "anexistinguserid",
            ProviderUId = "aprovideruid"
        };
        _ssoProvidersService.Setup(sps =>
                sps.FindUserByProviderAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<SSOAuthUserInfo>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(ssoUser.ToOptional());
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "amembershipsuserid",
                Status = EndUserStatus.Unregistered,
                Access = EndUserAccess.Enabled
            });
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result = await _service.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null, null, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _ssoProvidersService.Verify(sps =>
            sps.FindUserByProviderAsync(_caller.Object, "aprovidername", authUserInfo, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(
            eus => eus.RegisterPersonPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveInfoOnBehalfOfUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<SSOAuthUserInfo>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "anexistinguserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(
            ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthenticateAndPersonIsSuspended_ThenIssuesToken()
    {
        var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "auser@company.com", "afirstname", null,
            Timezones.Default,
            CountryCodes.Default);
        _ssoProvidersService.Setup(sps =>
                sps.AuthenticateUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authUserInfo);
        var ssoUser = new SSOUser
        {
            Id = "anexistinguserid",
            ProviderUId = "aprovideruid"
        };
        _ssoProvidersService.Setup(sps =>
                sps.FindUserByProviderAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<SSOAuthUserInfo>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(ssoUser.ToOptional());
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "amembershipsuserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Suspended
            });
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result = await _service.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null, null, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityLocked, Resources.SingleSignOnApplication_AccountSuspended);
        _ssoProvidersService.Verify(sps =>
            sps.FindUserByProviderAsync(_caller.Object, "aprovidername", authUserInfo, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(
            eus => eus.RegisterPersonPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveInfoOnBehalfOfUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<SSOAuthUserInfo>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "anexistinguserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(
            ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthenticateAndPersonNotExists_ThenRegistersPersonAndIssuesToken()
    {
        var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "auser@company.com", "afirstname", null,
            Timezones.Sydney,
            CountryCodes.Australia);
        _ssoProvidersService.Setup(sps =>
                sps.AuthenticateUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authUserInfo);
        _ssoProvidersService.Setup(sps =>
                sps.FindUserByProviderAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<SSOAuthUserInfo>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional.None<SSOUser>());
        _endUsersService.Setup(eus => eus.RegisterPersonPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithProfile
            {
                Id = "aregistereduserid"
            });
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "amembershipsuserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Enabled,
                Memberships =
                [
                    new Membership
                    {
                        Id = "amembershipid",
                        IsDefault = true,
                        OrganizationId = "anorganizationid",
                        UserId = "auserid"
                    }
                ]
            });
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result = await _service.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null, null, null, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _ssoProvidersService.Verify(sps =>
            sps.FindUserByProviderAsync(_caller.Object, "aprovidername", authUserInfo, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus => eus.RegisterPersonPrivateAsync(_caller.Object, "aninvitationtoken",
            "auser@company.com", "afirstname", null, Timezones.Sydney.ToString(), CountryCodes.Australia.ToString(),
            true, It.IsAny<CancellationToken>()));
        _ssoProvidersService.Verify(sps => sps.SaveInfoOnBehalfOfUserAsync(_caller.Object, "aprovidername",
            "aregistereduserid".ToId(),
            It.Is<SSOAuthUserInfo>(ui => ui == authUserInfo), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "aregistereduserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(ats =>
            ats.IssueTokensAsync(_caller.Object, "amembershipsuserid", null, null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAuthenticateAndPersonExists_ThenIssuesToken()
    {
        var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "auser@company.com", "afirstname", null,
            Timezones.Default,
            CountryCodes.Default);
        _ssoProvidersService.Setup(sps =>
                sps.AuthenticateUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authUserInfo);
        var ssoUser = new SSOUser
        {
            Id = "anexistinguserid",
            ProviderUId = "aprovideruid"
        };
        _ssoProvidersService.Setup(sps =>
                sps.FindUserByProviderAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<SSOAuthUserInfo>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(ssoUser.ToOptional());
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "amembershipsuserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Enabled,
                Memberships =
                [
                    new Membership
                    {
                        Id = "amembershipid",
                        IsDefault = true,
                        OrganizationId = "anorganizationid",
                        UserId = "auserid"
                    }
                ]
            });
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                }
            });

        var result = await _service.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null, null, null, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _ssoProvidersService.Verify(sps =>
            sps.FindUserByProviderAsync(_caller.Object, "aprovidername", authUserInfo, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(
            eus => eus.RegisterPersonPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(sps => sps.SaveInfoOnBehalfOfUserAsync(_caller.Object, "aprovidername",
            "anexistinguserid".ToId(),
            It.Is<SSOAuthUserInfo>(ui => ui == authUserInfo), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "anexistinguserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(ats =>
            ats.IssueTokensAsync(_caller.Object, "amembershipsuserid", null, null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenForUserAsyncAndProviderUserNotExists_ThenReturnsError()
    {
        _ssoProvidersService.Setup(sp =>
                sp.FindProviderByUserIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<ISSOAuthenticationProvider>.None);

        var result = await _service.RefreshTokenForUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        result.Error.AdditionalData.Should().OnlyContain(x =>
            x.Key == NativeIdentityServerSingleSignOnService.AuthErrorProviderName
            && (string)x.Value == "aprovidername");
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveTokensOnBehalfOfUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProviderAuthenticationTokens>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenForUserAsyncAndEndUserNotExists_ThenReturnsError()
    {
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _service.RefreshTokenForUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        result.Error.AdditionalData.Should().OnlyContain(x =>
            x.Key == NativeIdentityServerSingleSignOnService.AuthErrorProviderName
            && (string)x.Value == "aprovidername");
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvider.Verify(
            sop => sop.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveTokensOnBehalfOfUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProviderAuthenticationTokens>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenForUserAsyncAndPersonIsSuspended_ThenReturnsError()
    {
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Person,
                Access = EndUserAccess.Suspended
            });
        _ssoProvider.Setup(sp =>
                sp.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("amessage"));

        var result = await _service.RefreshTokenForUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.SingleSignOnApplication_AccountSuspended);
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvider.Verify(
            sop => sop.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveTokensOnBehalfOfUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProviderAuthenticationTokens>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenForUserAsyncAndRefreshErrors_ThenReturnsError()
    {
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Person,
                Access = EndUserAccess.Enabled
            });
        _ssoProvider.Setup(sp =>
                sp.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("amessage"));

        var result = await _service.RefreshTokenForUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        result.Error.AdditionalData.Should().OnlyContain(x =>
            x.Key == NativeIdentityServerSingleSignOnService.AuthErrorProviderName
            && (string)x.Value == "aprovidername");
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvider.Verify(
            sop => sop.RefreshTokenAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvidersService.Verify(
            sps => sps.SaveTokensOnBehalfOfUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProviderAuthenticationTokens>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenForUserAsync_ThenRefreshed()
    {
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Person,
                Access = EndUserAccess.Enabled
            });
        _ssoProvider.Setup(sp =>
                sp.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderAuthenticationTokens
            {
                Provider = "aprovidername",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = default,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = default,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                OtherTokens =
                [
                    new AuthenticationToken
                    {
                        ExpiresOn = default,
                        Type = TokenType.OtherToken,
                        Value = "anothertoken"
                    }
                ]
            });

        var result = await _service.RefreshTokenForUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Provider.Should().Be("aprovidername");
        result.Value.AccessToken.Type.Should().Be(TokenType.AccessToken);
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.RefreshToken!.Type.Should().Be(TokenType.RefreshToken);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.OtherTokens.Count.Should().Be(1);
        result.Value.OtherTokens[0].Type.Should().Be(TokenType.OtherToken);
        result.Value.OtherTokens[0].Value.Should().Be("anothertoken");
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvider.Verify(
            sop => sop.RefreshTokenAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvidersService.Verify(
            sps => sps.SaveTokensOnBehalfOfUserAsync(_caller.Object, "aprovidername", "auserid".ToId(),
                It.Is<ProviderAuthenticationTokens>(token =>
                    token.Provider == "aprovidername"
                    && token.AccessToken.Value == "anaccesstoken"
                    && token.RefreshToken!.Value == "arefreshtoken"
                    && token.OtherTokens.Count == 1
                    && token.OtherTokens[0].Value == "anothertoken"
                ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetTokensForUserAsync_ThenReturnsTokens()
    {
        var datum = DateTime.UtcNow;
        _ssoProvidersService.Setup(sps =>
                sps.GetTokensForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderAuthenticationTokens>
            {
                new()
                {
                    Provider = "aprovidername",
                    AccessToken = new AuthenticationToken
                    {
                        ExpiresOn = datum,
                        Type = TokenType.AccessToken,
                        Value = "anaccesstoken"
                    },
                    RefreshToken = new AuthenticationToken
                    {
                        ExpiresOn = datum,
                        Type = TokenType.RefreshToken,
                        Value = "arefreshtoken"
                    },
                    OtherTokens =
                    [
                        new AuthenticationToken
                        {
                            ExpiresOn = datum,
                            Type = TokenType.OtherToken,
                            Value = "anothertoken"
                        }
                    ]
                }
            });

        var result = await _service.GetTokensForUserAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value[0].Provider.Should().Be("aprovidername");
        result.Value[0].AccessToken.ExpiresOn.Should().Be(datum);
        result.Value[0].AccessToken.Type.Should().Be(TokenType.AccessToken);
        result.Value[0].AccessToken.Value.Should().Be("anaccesstoken");
        result.Value[0].RefreshToken!.ExpiresOn.Should().Be(datum);
        result.Value[0].RefreshToken!.Type.Should().Be(TokenType.RefreshToken);
        result.Value[0].RefreshToken!.Value.Should().Be("arefreshtoken");
        result.Value[0].OtherTokens.Count.Should().Be(1);
        result.Value[0].OtherTokens[0].ExpiresOn.Should().Be(datum);
        result.Value[0].OtherTokens[0].Type.Should().Be(TokenType.OtherToken);
        result.Value[0].OtherTokens[0].Value.Should().Be("anothertoken");
    }
}