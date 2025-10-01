using Application.Interfaces;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Web.Hosting.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Auth;

[Trait("Category", "Unit")]
public class AllAuthorizationPoliciesProviderSpec
{
    private readonly AllAuthorizationPoliciesProvider _provider;

    public AllAuthorizationPoliciesProviderSpec()
    {
        var options = new Mock<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>();
        options.Setup(opt => opt.Value).Returns(new Microsoft.AspNetCore.Authorization.AuthorizationOptions());
        _provider = new AllAuthorizationPoliciesProvider(options.Object);
    }

    [Fact]
    public async Task WhenGetPolicyAsyncAndNotCachedAndRolesAndFeaturesPolicy_ThenBuildsPolicy()
    {
        var policyName =
            $"{AuthenticationConstants.Authorization.RolesAndFeaturesPolicyName}:{{|Features|:{{|Platform|:[|{PlatformFeatures.Basic.Name}|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}";

        var result = await _provider.GetPolicyAsync(policyName);

        result!.Requirements.Count.Should().Be(2);
        result.Requirements[1].Should().BeOfType<RolesAndFeaturesAuthorizationRequirement>();
        result.Requirements[1].As<RolesAndFeaturesAuthorizationRequirement>().Roles.All.Should()
            .ContainSingle(PlatformRoles.Standard.Name);
        result.Requirements[1].As<RolesAndFeaturesAuthorizationRequirement>().Features.All.Should()
            .ContainSingle(PlatformFeatures.Basic.Name);

#if TESTINGONLY
        _provider.IsCached(policyName).Should().BeTrue();
#endif
    }

    [Fact]
    public async Task WhenGetPolicyAsyncAndCached_ThenReturnsCachedPolicy()
    {
        var builder = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build();
        var policyName =
            $"{AuthenticationConstants.Authorization.RolesAndFeaturesPolicyName}:{{|Features|:{{|Platform|:[|basic_features|]}},|Roles|:{{|Platform|:[|{{{PlatformRoles.Standard.Name}}}|]}}}}";
#if TESTINGONLY
        _provider.CachePolicy(policyName, builder);
#endif

        var result = await _provider.GetPolicyAsync(policyName);

        result.Should().Be(builder);

#if TESTINGONLY
        _provider.IsCached(policyName).Should().BeTrue();
#endif
    }

    [Fact]
    public async Task WhenGetPolicyAsyncAndNotCachedAndAnyOtherPolicy_ThenBuildsDummyPolicy()
    {
        var policyName = "apolicyname";

        var result = await _provider.GetPolicyAsync(policyName);

        result!.Requirements.Count.Should().Be(1);
        result.Requirements[0].Should().BeOfType<AssertionRequirement>();

#if TESTINGONLY
        _provider.IsCached(policyName).Should().BeTrue();
#endif
    }
}