#if TESTINGONLY
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Application.Resources.Shared;
using Common.Configuration;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Services.Shared;
using FluentAssertions;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Hosting.Common;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace ApiHost1.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class AuthZApiSpec : WebApiSpec<Program>
{
    private readonly IConfigurationSettings _settings;
    private readonly ITokensService _tokensService;

    public AuthZApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
        EmptyAllRepositories();
        _settings = setup.GetRequiredService<IConfigurationSettings>();
        _tokensService = setup.GetRequiredService<ITokensService>();
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousWithNoProof_ThenReturns200()
    {
        var result = await Api.GetAsync(new AuthorizeByAnonymousTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(CallerConstants.AnonymousUserId);
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousWithUnsignedToken_ThenReturns401()
    {
        var login = await LoginUserAsync();

        //Unpack this token, and repack it without signing it again
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(login.AccessToken);
        var unSignedAccessToken = handler.WriteToken(token);

        var result = await Api.GetAsync(new AuthorizeByAnonymousTestingOnlyRequest(),
            req => req.SetJWTBearerToken(unSignedAccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByNothingWithExpiredToken_ThenReturns401()
    {
        var login = await LoginUserAsync();

        //Unpack this token, change the expiry date to the past, repack, and sign it
        var signingCredentials =
#if TESTINGONLY
            JWTTokensService.GetSigningCredentials(_settings);
#else
            ((Microsoft.IdentityModel.Tokens.SigningCredentials)null!);
#endif
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(login.AccessToken);
        var expiredAccessToken = handler.WriteToken(new JwtSecurityToken(
            claims: token.Claims,
            expires: DateTime.UtcNow.AddHours(-1),
            issuer: token.Issuer,
            audience: token.Audiences.First(),
            signingCredentials: signingCredentials
        ));

        var result = await Api.GetAsync(new AuthorizeByAnonymousTestingOnlyRequest(),
            req => req.SetJWTBearerToken(expiredAccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousWithValidToken_ThenReturns200()
    {
        var login = await LoginUserAsync();

        var result = await Api.GetAsync(new AuthorizeByAnonymousTestingOnlyRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(login.User.Id);
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousWithInvalidHMacSignature_ThenReturns401()
    {
        var request = new AuthorizeByAnonymousTestingOnlyRequest();

        var result = await Api.GetAsync(request,
            req => req.SetHMACAuth("awrongsecret"));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousWithValidHMacSignature_ThenReturns200()
    {
        var request = new AuthorizeByAnonymousTestingOnlyRequest();

        var result = await Api.GetAsync(request,
            req => req.SetHMACAuth("asecret"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(CallerConstants.MaintenanceAccountUserId);
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousWithUnknownAPIKey_ThenReturns401()
    {
        var apiKey = new TokensService().CreateAPIKey().ApiKey;
        var result = await Api.GetAsync(new AuthorizeByAnonymousTestingOnlyRequest(),
            req => req.SetAPIKey(apiKey));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousWithKnownAPIKey_ThenReturns200()
    {
        var login = await LoginUserAsync();
        var apiKey = (await Api.PostAsync(new CreateAPIKeyRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.ApiKey;

        var result = await Api.GetAsync(new AuthorizeByAnonymousTestingOnlyRequest(),
            req => req.SetAPIKey(apiKey));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(login.User.Id);
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousAndNoRolesOrFeatures_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService,
            [], []);

        var result = await Api.GetAsync(new AuthorizeByAnonymousTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousAndAnyRoles_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService,
            [PlatformRoles.Standard, PlatformRoles.Operations], []);

        var result = await Api.GetAsync(new AuthorizeByAnonymousTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    [Fact]
    public async Task WhenAuthorizeByAnonymousAndAnyFeatures_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService,
            [],
            [PlatformFeatures.PaidTrial, PlatformFeatures.Basic]);

        var result = await Api.GetAsync(new AuthorizeByAnonymousTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndRoleRequestWithNoToken_ThenReturns401()
    {
        var result = await Api.GetAsync(new AuthorizeByTokenWithRoleTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndRoleRequestWithUnsignedToken_ThenReturns401()
    {
        var login = await LoginUserAsync();

        //Unpack this token, and repack it without signing it again
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(login.AccessToken);
        var unSignedAccessToken = handler.WriteToken(token);

        var result = await Api.GetAsync(new AuthorizeByTokenWithRoleTestingOnlyRequest(),
            req => req.SetJWTBearerToken(unSignedAccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndRoleRequestWithExpiredToken_ThenReturns401()
    {
        var login = await LoginUserAsync();

        //Unpack this token, change the expiry date to the past, repack, and sign it
        var signingCredentials =
#if TESTINGONLY
            JWTTokensService.GetSigningCredentials(_settings);
#else
            ((Microsoft.IdentityModel.Tokens.SigningCredentials)null!);
#endif
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(login.AccessToken);
        var expiredAccessToken = handler.WriteToken(new JwtSecurityToken(
            claims: token.Claims,
            expires: DateTime.UtcNow.AddHours(-1),
            issuer: token.Issuer,
            audience: token.Audiences.First(),
            signingCredentials: signingCredentials
        ));

        var result = await Api.GetAsync(new AuthorizeByTokenWithRoleTestingOnlyRequest(),
            req => req.SetJWTBearerToken(expiredAccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenWithInvalidHMacSignature_ThenReturns401()
    {
        var request = new AuthorizeByTokenWithRoleTestingOnlyRequest();

        var result = await Api.GetAsync(request,
            req => req.SetHMACAuth("awrongsecret"));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenWithInvalidAPIKey_ThenReturns401()
    {
        var result = await Api.GetAsync(new AuthorizeByTokenWithRoleTestingOnlyRequest(),
            req => req.SetAPIKey("awrongapikey"));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndRoleRequestWithNoRole_ThenReturns403()
    {
        var token = CreateJwtToken(_settings, _tokensService, [], []);

        var result = await Api.GetAsync(new AuthorizeByTokenWithRoleTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndRoleRequestWithWrongRole_ThenReturns403()
    {
        var token = CreateJwtToken(_settings, _tokensService, [new RoleLevel("awrongrole")],
            []);

        var result = await Api.GetAsync(new AuthorizeByTokenWithRoleTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndRoleRequestIncludingCorrectRole_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService,
            [PlatformRoles.Standard, PlatformRoles.Operations], [PlatformFeatures.Basic]);

        var result = await Api.GetAsync(new AuthorizeByTokenWithRoleTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndFeatureRequestWithNoToken_ThenReturns401()
    {
        var result = await Api.GetAsync(new AuthorizeByFeatureTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndFeatureRequestWithNoFeature_ThenReturns403()
    {
        var token = CreateJwtToken(_settings, _tokensService, [], []);

        var result = await Api.GetAsync(new AuthorizeByFeatureTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndFeatureRequestWithWrongFeature_ThenReturns403()
    {
        var token = CreateJwtToken(_settings, _tokensService, [PlatformRoles.Standard],
            [new FeatureLevel("awrongfeature")]);

        var result = await Api.GetAsync(new AuthorizeByFeatureTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task WhenAuthorizeByTokenAndFeatureRequestIncludingCorrectFeature_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService, [PlatformRoles.Standard],
            [PlatformFeatures.PaidTrial]);

        var result = await Api.GetAsync(new AuthorizeByFeatureTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    private static string CreateJwtToken(IConfigurationSettings settings, ITokensService tokensService,
        List<RoleLevel> platFormRoles, List<FeatureLevel> platformFeatures)
    {
        return new JWTTokensService(settings, tokensService)
            .IssueTokensAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Roles = platFormRoles.Select(rol => rol.Name).ToList(),
                Features = platformFeatures.Select(rol => rol.Name).ToList()
            }, null, null, null).GetAwaiter().GetResult().Value.AccessToken;
    }
}
#endif