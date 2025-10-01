using Application.Interfaces;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Web.Hosting.Common.Auth;

/// <summary>
///     Provides an authorization handler that processes an authorization requirement
/// </summary>
public sealed class
    RolesAndFeaturesAuthorizationHandler : AuthorizationHandler<RolesAndFeaturesAuthorizationRequirement>
{
    private readonly ICallerContextFactory _callerFactory;

    public RolesAndFeaturesAuthorizationHandler(ICallerContextFactory callerFactory)
    {
        _callerFactory = callerFactory;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        RolesAndFeaturesAuthorizationRequirement requirement)
    {
        var caller = _callerFactory.Create();

        foreach (var platformRole in requirement.Roles.Platform)
        {
            if (!caller.Roles.Platform.ToList()
                    .Any(rol => rol == platformRole || rol.HasDescendant(platformRole)))
            {
                context.Fail(new AuthorizationFailureReason(this,
                    Resources.RolesAndFeaturesAuthorizationHandler_HandleRequirementAsync_MissingRole));
                return Task.CompletedTask;
            }
        }

        foreach (var tenantRole in requirement.Roles.Tenant)
        {
            if (!caller.Roles.Tenant.ToList()
                    .Any(rol => rol == tenantRole || rol.HasDescendant(tenantRole)))
            {
                context.Fail(new AuthorizationFailureReason(this,
                    Resources.RolesAndFeaturesAuthorizationHandler_HandleRequirementAsync_MissingRole));
                return Task.CompletedTask;
            }
        }

        foreach (var platformFeature in requirement.Features.Platform)
        {
            if (!caller.Features.Platform.ToList()
                    .Any(feat => feat == platformFeature || feat.HasDescendant(platformFeature)))
            {
                context.Fail(new AuthorizationFailureReason(this,
                    Resources.RolesAndFeaturesAuthorizationHandler_HandleRequirementAsync_MissingFeature));
                return Task.CompletedTask;
            }
        }

        foreach (var tenantFeature in requirement.Features.Tenant)
        {
            if (!caller.Features.Tenant.ToList()
                    .Any(feat => feat == tenantFeature || feat.HasDescendant(tenantFeature)))
            {
                context.Fail(new AuthorizationFailureReason(this,
                    Resources.RolesAndFeaturesAuthorizationHandler_HandleRequirementAsync_MissingFeature));
                return Task.CompletedTask;
            }
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
///     Provides an authorization requirement that will be asserted to authorize a request
/// </summary>
public sealed class RolesAndFeaturesAuthorizationRequirement : IAuthorizationRequirement
{
    public RolesAndFeaturesAuthorizationRequirement(ICallerContext.CallerRoles roles,
        ICallerContext.CallerFeatures features)
    {
        Roles = roles;
        Features = features;
    }

    public ICallerContext.CallerFeatures Features { get; }

    public ICallerContext.CallerRoles Roles { get; }
}