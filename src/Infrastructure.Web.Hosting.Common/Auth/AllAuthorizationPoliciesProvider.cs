using System.Collections.Concurrent;
using Application.Interfaces;
using Common.Extensions;
using Infrastructure.Web.Hosting.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using AuthorizeAttribute = Infrastructure.Web.Api.Interfaces.AuthorizeAttribute;

namespace Infrastructure.Web.Hosting.Common.Auth;

/// <summary>
///     Provides an authorization policy provider for configuring all authorization policies,
///     that are configured on minimal API endpoints. Specifically, used here for dynamically configuring the
///     <see cref="RolesAndFeaturesAuthorizationRequirement" /> which is code generated onto minimal API endpoints,
///     but cannot be declarative in the <see cref="HostExtensions.ConfigureApiHost" /> method.
///     Note: it is possible that a policy is code generated onto a minimal API endpoint, but that policy is not configured
///     in the host in <see cref="HostExtensions.ConfigureApiHost" />, since it was not intended to be used in that
///     specific host. In which case, we need to dynamically build it here - to do nothing.
/// </summary>
public sealed class AllAuthorizationPoliciesProvider : DefaultAuthorizationPolicyProvider
{
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policyCache = new();

    public AllAuthorizationPoliciesProvider(
        IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (_policyCache.TryGetValue(policyName, out var cachedPolicy))
        {
            return cachedPolicy;
        }

        var policy = await base.GetPolicyAsync(policyName);
        if (policy.Exists())
        {
            return policy;
        }

        //If we got here, it means that it is a dynamic policy that we need to build,
        //or one that is defined on an endpoint, but not configured in the host
        var builder = new AuthorizationPolicyBuilder();
        if (policyName.StartsWith(AuthenticationConstants.Authorization.RolesAndFeaturesPolicyName))
        {
            var rolesAndFeatures = AuthorizeAttribute.ParsePolicyName(policyName);
            var roleAndFeaturesRequirements = rolesAndFeatures
                .Select(rf => new RolesAndFeaturesAuthorizationRequirement(rf.Roles, rf.Features))
                .Cast<IAuthorizationRequirement>().ToArray();

            if (roleAndFeaturesRequirements.HasAny())
            {
                builder.AddRequirements(new DenyAnonymousAuthorizationRequirement());
                builder.AddRequirements(roleAndFeaturesRequirements);
            }
        }
        else
        {
            // Just return a policy that always authorizes
            builder.AddRequirements(new AssertionRequirement(_ => true));
        }

        var policies = builder.Build();
        _policyCache.TryAdd(policyName, policies);

        return policies;
    }

#if TESTINGONLY
    internal bool IsCached(string policyName)
    {
        return _policyCache.TryGetValue(policyName, out _);
    }

    internal void CachePolicy(string policyName, AuthorizationPolicy builder)
    {
        _policyCache.TryAdd(policyName, builder);
    }
#endif
}