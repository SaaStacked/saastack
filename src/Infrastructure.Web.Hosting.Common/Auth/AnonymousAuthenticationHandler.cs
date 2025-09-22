using Common.Extensions;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common.Auth;

/// <summary>
///     Provides a <see cref="AuthorizationHandler" /> for authorizing anonymous access with
///     any provided auth proof (e.g. a JWT token, apikey, hmac, or beffe authn cookie).
///     Note: By default, in ASPNET if the minimal API endpoint is marked as Anonymous, then none of the authentication
///     handlers will be run when a request comes in, since they are not required to run (i.e., absence of the
///     'RequireAuthorization()' method being code-generated on them).
///     This means, that if for example, a JWT token is provided in the anonymous request, that JWT token will not be
///     validated, and it could be expired, faked or tampered with. We definitely want to reject the request in this case.
///     We have defined an alternative authorization policy called
///     <see cref="AuthenticationConstants.Authorization.AnonymousPolicyName" />, that is applied to all anonymous
///     minimal API endpoints by default (i.e. code-generates 'RequireAuthorization("Anonymous")'),
///     so that if any auth proof is provided in the request, that proof will be force-validated
///     by calling the respective <see cref="IAuthenticationHandler" />.
///     Note: If the proof is, in fact valid, it should also be extracted and the claims used to populate the
///     <see cref="ICallerContext" /> as usual, in cases where hte API endpoint works for both
///     anonymous and authenticated callers.
/// </summary>
public class AnonymousAuthenticationHandler : AuthorizationHandler<AnonymousAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        AnonymousAuthorizationRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Succeed(requirement);
            return;
        }

        var authHandlerProvider = httpContext.RequestServices.GetService<IAuthenticationHandlerProvider>();
        if (authHandlerProvider.NotExists())
        {
            context.Succeed(requirement);
            return;
        }

        if (await ValidateJwtTokenIfExists(context, requirement, httpContext, authHandlerProvider))
        {
            return;
        }

        if (await ValidateApiKeyIfExists(context, requirement, httpContext, authHandlerProvider))
        {
            return;
        }

        if (await ValidateHMacSignatureIfExists(context, requirement, httpContext, authHandlerProvider))
        {
            return;
        }

        if (await ValidateBeffeAuthNCookieIfExists(context, requirement, httpContext, authHandlerProvider))
        {
            return;
        }

        // No auth = allow anonymous
        context.Succeed(requirement);
    }

    private async Task<bool> ValidateJwtTokenIfExists(AuthorizationHandlerContext context,
        AnonymousAuthorizationRequirement requirement,
        HttpContext httpContext, IAuthenticationHandlerProvider authHandlerProvider)
    {
        var token = httpContext.Request.GetTokenAuth();
        if (token.HasValue)
        {
            var jwtBearerHandler =
                await authHandlerProvider.GetHandlerAsync(httpContext, JwtBearerDefaults.AuthenticationScheme);
            if (jwtBearerHandler.NotExists())
            {
                context.Succeed(requirement);
                return true;
            }

            var result = await jwtBearerHandler.AuthenticateAsync();
            if (result.Succeeded)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, result.Failure!.Message));
            }

            return true;
        }

        return false;
    }

    private async Task<bool> ValidateApiKeyIfExists(AuthorizationHandlerContext context,
        AnonymousAuthorizationRequirement requirement,
        HttpContext httpContext, IAuthenticationHandlerProvider authHandlerProvider)
    {
        var apiKey = httpContext.Request.GetAPIKeyAuth();
        if (apiKey.HasValue)
        {
            var apiKeyHandler =
                await authHandlerProvider.GetHandlerAsync(httpContext,
                    APIKeyAuthenticationHandler.AuthenticationScheme);
            if (apiKeyHandler.NotExists())
            {
                context.Succeed(requirement);
                return true;
            }

            var result = await apiKeyHandler.AuthenticateAsync();
            if (result.Succeeded)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, result.Failure!.Message));
            }

            return true;
        }

        return false;
    }

    private async Task<bool> ValidateHMacSignatureIfExists(AuthorizationHandlerContext context,
        AnonymousAuthorizationRequirement requirement, HttpContext httpContext,
        IAuthenticationHandlerProvider authHandlerProvider)
    {
        var hmac = httpContext.Request.GetHMACAuth();
        if (hmac.HasValue)
        {
            var hmacHandler =
                await authHandlerProvider.GetHandlerAsync(httpContext, HMACAuthenticationHandler.AuthenticationScheme);
            if (hmacHandler.NotExists())
            {
                context.Succeed(requirement);
                return true;
            }

            var result = await hmacHandler.AuthenticateAsync();
            if (result.Succeeded)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, result.Failure!.Message));
            }

            return true;
        }

        return false;
    }

    private async Task<bool> ValidateBeffeAuthNCookieIfExists(AuthorizationHandlerContext context,
        AnonymousAuthorizationRequirement requirement, HttpContext httpContext,
        IAuthenticationHandlerProvider authHandlerProvider)
    {
        var cookieClaims = httpContext.Request.GetAuthNCookie();
        if (cookieClaims.HasValue)
        {
            var cookieHandler =
                await authHandlerProvider.GetHandlerAsync(httpContext,
                    BeffeCookieAuthenticationHandler.AuthenticationScheme);
            if (cookieHandler.NotExists())
            {
                context.Succeed(requirement);
                return true;
            }

            var result = await cookieHandler.AuthenticateAsync();
            if (result.Succeeded)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, result.Failure!.Message));
            }

            return true;
        }

        return false;
    }
}

/// <summary>
///     Defines the custom requirement for the <see cref="AnonymousAuthenticationHandler" /> authorization
///     handler
/// </summary>
public class AnonymousAuthorizationRequirement : IAuthorizationRequirement
{
}