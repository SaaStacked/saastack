using Application.Interfaces;
using Application.Persistence.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared.Identities;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class NativeIdentityServerApiKeyServiceSpec
{
    private readonly Mock<IAPIKeyHasherService> _apiKeyHasherService;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IAPIKeysRepository> _repository;
    private readonly NativeIdentityServerApiKeyService _service;
    private readonly Mock<ITokensService> _tokensService;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public NativeIdentityServerApiKeyServiceSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateAPIKey())
            .Returns(new APIKeyToken
            {
                Key = "akey",
                Prefix = "aprefix",
                Token = "atoken",
                ApiKey = "anapikey"
            });
        _tokensService.Setup(ts => ts.ParseApiKey(It.IsAny<string>()))
            .Returns(new APIKeyToken
            {
                Key = "akey",
                Prefix = "aprefix",
                Token = "atoken",
                ApiKey = "anapikey"
            });
        _apiKeyHasherService = new Mock<IAPIKeyHasherService>();
        _apiKeyHasherService.Setup(khs => khs.HashAPIKey(It.IsAny<string>()))
            .Returns("akeyhash");
        _apiKeyHasherService.Setup(khs => khs.ValidateAPIKeyHash(It.IsAny<string>()))
            .Returns(true);
        _endUsersService = new Mock<IEndUsersService>();
        _userProfilesService = new Mock<IUserProfilesService>();
        _repository = new Mock<IAPIKeysRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<APIKeyRoot>(), It.IsAny<CancellationToken>()))
            .Returns((APIKeyRoot root, CancellationToken _) => Task.FromResult<Result<APIKeyRoot, Error>>(root));

        _service = new NativeIdentityServerApiKeyService(_recorder.Object, idFactory.Object, _tokensService.Object,
            _apiKeyHasherService.Object, _endUsersService.Object, _userProfilesService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndNotAValidApiKey_ThenReturnsError()
    {
        _tokensService.Setup(ts => ts.ParseApiKey(It.IsAny<string>()))
            .Returns(Optional<APIKeyToken>.None);
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<APIKeyRoot>.None);

        var result =
            await _service.AuthenticateAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndApiKeyNotExist_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<APIKeyRoot>.None);

        var result =
            await _service.AuthenticateAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndUserNotExist_ThenReturnsError()
    {
        var apiKey = CreateApiKey();
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey.ToOptional());
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result =
            await _service.AuthenticateAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _endUsersService.Verify(
            eus => eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", CancellationToken.None));
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndExpired_ThenReturnsError()
    {
        var apiKey = CreateApiKey();
        apiKey.ForceExpire("auserid".ToId());
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey.ToOptional());

        var result =
            await _service.AuthenticateAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _endUsersService.Verify(
            eus => eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndUserNotRegistered_ThenReturnsError()
    {
        var apiKey = CreateApiKey();
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey.ToOptional());
        var user = new EndUserWithMemberships
        {
            Id = "auserid",
            Status = EndUserStatus.Unregistered
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result =
            await _service.AuthenticateAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _endUsersService.Verify(
            eus => eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", CancellationToken.None));
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndUserIsSuspended_ThenReturnsError()
    {
        var apiKey = CreateApiKey();
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey.ToOptional());
        var user = new EndUserWithMemberships
        {
            Id = "auserid",
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Suspended
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result =
            await _service.AuthenticateAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.NativeIdentityServerApiKeyService_AccountSuspended);
        _endUsersService.Verify(
            eus => eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", CancellationToken.None));
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "auserid",
            Audits.APIKeysApplication_Authenticate_AccountSuspended, It.IsAny<string>(),
            It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenAuthenticateAsync_ThenAuthenticates()
    {
        _caller.Setup(cc => cc.CallId)
            .Returns("acallid");
        var apiKey = CreateApiKey();
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey.ToOptional());
        var user = new EndUserWithMemberships
        {
            Id = "auserid",
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
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "aprofileid",
                UserId = "auserid",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                DisplayName = "adisplayname"
            });

        var result =
            await _service.AuthenticateAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Value.Id.Should().Be("auserid");
        _endUsersService.Verify(
            eus => eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", CancellationToken.None));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc =>
                cc.CallId == "acallid"
            ), "auserid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCreateApiKeyWithNoInformationAsync_ThenCreates()
    {
        _repository.Setup(rep =>
                rep.SearchAllUnexpiredForUserAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<APIKeyAuth>([]));

        var result =
            await _service.CreateAPIKeyForUserAsync(_caller.Object, "auserid", "adescription", null,
                CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        result.Value.Key.Should().Be("anapikey");
        result.Value.UserId.Should().Be("auserid");
        result.Value.Description.Should().Be("adescription");
        result.Value.ExpiresOnUtc.Should().BeNull();
        _tokensService.Verify(ts => ts.CreateAPIKey());
        _repository.Verify(rep => rep.SaveAsync(It.Is<APIKeyRoot>(ak =>
            ak.ApiKey.Value.Token == "atoken"
            && ak.ApiKey.Value.KeyHash == "akeyhash"
            && ak.UserId == "auserid"
            && ak.Description == "adescription"
            && ak.ExpiresOn == Optional<DateTime>.None
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCreateAPIKeyForUserAsyncWithExistingApiKey_ThenCreatesNewAndExpiresExisting()
    {
        var expiresOn = DateTime.UtcNow.AddHours(1);
        _repository.Setup(rep =>
                rep.SearchAllUnexpiredForUserAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<APIKeyAuth>([
                new APIKeyAuth
                {
                    Id = "anid",
                    ExpiresOn = DateTime.UtcNow.AddHours(1),
                    UserId = "auserid"
                },

                new APIKeyAuth
                {
                    Id = "anapikeyid1",
                    ExpiresOn = DateTime.UtcNow.AddHours(1),
                    UserId = "auserid"
                },

                new APIKeyAuth
                {
                    Id = "anapikeyid2",
                    ExpiresOn = DateTime.UtcNow.SubtractHours(1),
                    UserId = "auserid"
                }
            ]));
        _repository.Setup(rep => rep.LoadAsync("anapikeyid1".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiKey("anapikeyid1"));
        _repository.Setup(rep => rep.LoadAsync("anapikeyid2".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiKey("anapikeyid2"));

        var result =
            await _service.CreateAPIKeyForUserAsync(_caller.Object, "auserid", "adescription", expiresOn,
                CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        result.Value.Key.Should().Be("anapikey");
        result.Value.UserId.Should().Be("auserid");
        result.Value.Description.Should().Be("adescription");
        result.Value.ExpiresOnUtc.Should().Be(expiresOn);
        _tokensService.Verify(ts => ts.CreateAPIKey());
        _repository.Verify(rep => rep.LoadAsync("anid".ToId(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(rep => rep.SaveAsync(It.Is<APIKeyRoot>(ak =>
            ak.ApiKey.Value.Token == "atoken"
            && ak.ApiKey.Value.KeyHash == "akeyhash"
            && ak.Description == "adescription"
            && ak.UserId == "auserid"
            && ak.ExpiresOn.Value == expiresOn
        ), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(rep => rep.LoadAsync("anapikeyid1".ToId(), It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.Is<APIKeyRoot>(ak =>
            ak.Id == "anapikeyid1"
            && ak.UserId == "auserid"
            && ak.ExpiresOn.Value < DateTime.UtcNow
        ), It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.Is<APIKeyRoot>(ak =>
            ak.Id == "anapikeyid2"
            && ak.UserId == "auserid"
            && ak.ExpiresOn.Value < DateTime.UtcNow
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCreateAPIKeyForUserAsync_ThenCreatesNew()
    {
        var expiresOn = DateTime.UtcNow.AddHours(1);
        _repository.Setup(rep =>
                rep.SearchAllUnexpiredForUserAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<APIKeyAuth>([]));

        var result =
            await _service.CreateAPIKeyForUserAsync(_caller.Object, "auserid", "adescription", expiresOn,
                CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        result.Value.Key.Should().Be("anapikey");
        result.Value.UserId.Should().Be("auserid");
        result.Value.Description.Should().Be("adescription");
        result.Value.ExpiresOnUtc.Should().Be(expiresOn);
        _tokensService.Verify(ts => ts.CreateAPIKey());
        _repository.Verify(rep => rep.SaveAsync(It.Is<APIKeyRoot>(ak =>
            ak.ApiKey.Value.Token == "atoken"
            && ak.ApiKey.Value.KeyHash == "akeyhash"
            && ak.Description == "adescription"
            && ak.UserId == "auserid"
            && ak.ExpiresOn.Value == expiresOn
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchAllAPIKeysAsync_ThenReturnsAll()
    {
        var expiresOn = DateTime.UtcNow.AddHours(1);
        _repository.Setup(rep => rep.SearchAllForUserAsync(It.IsAny<Identifier>(), It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<APIKeyAuth>([
                new APIKeyAuth
                {
                    Id = "anid",
                    KeyToken = "akeytoken",
                    UserId = "auserid",
                    Description = "adescription",
                    ExpiresOn = expiresOn
                }
            ]));

        var result = await _service.SearchAllAPIKeysForUserAsync(_caller.Object, "auserid", new SearchOptions(),
            new GetOptions(), CancellationToken.None);

        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("anid");
        result.Value.Results[0].Key.Should().Be("akeytoken");
        result.Value.Results[0].UserId.Should().Be("auserid");
        result.Value.Results[0].Description.Should().Be("adescription");
        result.Value.Results[0].ExpiresOnUtc.Should().Be(expiresOn);
        _repository.Verify(rep =>
            rep.SearchAllForUserAsync("auserid".ToId(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeleteAPIKeyForUserAsyncAndNotExist_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _service.DeleteAPIKeyForUserAsync(_caller.Object, "anid", "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDeleteAPIKeyForUserAsync_ThenDeletes()
    {
        var apiKey = CreateApiKey();
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey);

        var result = await _service.DeleteAPIKeyForUserAsync(_caller.Object, "anid", "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<APIKeyRoot>(key =>
            key.IsDeleted
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRevokeAPIKeyAsyncAndNotExist_ThenReturnsError()
    {
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _service.RevokeAPIKeyAsync(_caller.Object, "anid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenRevokeAPIKeyAsync_ThenRevokes()
    {
        var apiKey = CreateApiKey();
        _repository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey);
        _caller.Setup(cc => cc.Roles).Returns(new ICallerContext.CallerRoles([PlatformRoles.Operations], []));

        var result = await _service.RevokeAPIKeyAsync(_caller.Object, "anid", CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<APIKeyRoot>(key =>
            key.RevokedOn.Value.IsNear(DateTime.UtcNow)
        ), It.IsAny<CancellationToken>()));
    }

    private APIKeyRoot CreateApiKey(string apikeyId = "anid")
    {
        return APIKeyRoot.Create(_recorder.Object, apikeyId.ToIdentifierFactory(), _apiKeyHasherService.Object,
            "auserid".ToId(), new APIKeyToken
            {
                Key = "akey",
                Prefix = "aprefix",
                Token = "atoken",
                ApiKey = "anapikey"
            }).Value;
    }
}