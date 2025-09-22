using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Hosting.Common.Auth;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Auth;

[Trait("Category", "Unit")]
public class AnonymousAuthenticationHandlerSpec
{
    private readonly AnonymousAuthenticationHandler _handler;
    private readonly AnonymousAuthorizationRequirement _requirement;

    public AnonymousAuthenticationHandlerSpec()
    {
        _handler = new AnonymousAuthenticationHandler();
        _requirement = new AnonymousAuthorizationRequirement();
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndNoProofs_ThenSucceeds()
    {
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, null);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndNoAuthenticationProvider_ThenSucceeds()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationHandlerProvider)))
            .Returns((IAuthenticationHandlerProvider?)null);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider.Object
        };
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, httpContext);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndInvalidJwtToken_ThenFails()
    {
        var handler = new Mock<IAuthenticationHandler>();
        handler.Setup(h => h.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.Fail("amessage"));
        var authProvider = new Mock<IAuthenticationHandlerProvider>();
        authProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(handler.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationHandlerProvider)))
            .Returns(authProvider.Object);

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HttpConstants.Headers.Authorization] = "Bearer atoken"
                }
            },
            RequestServices = serviceProvider.Object
        };
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, httpContext);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.FailureReasons.Should().OnlyContain(r => r.Message == "amessage");
        authProvider.Verify(ap => ap.GetHandlerAsync(httpContext, JwtBearerDefaults.AuthenticationScheme));
        handler.Verify(h => h.AuthenticateAsync());
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndValidJwtToken_ThenSucceeds()
    {
        var handler = new Mock<IAuthenticationHandler>();
        handler.Setup(h => h.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "ascheme")));
        var authProvider = new Mock<IAuthenticationHandlerProvider>();
        authProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(handler.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationHandlerProvider)))
            .Returns(authProvider.Object);

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HttpConstants.Headers.Authorization] = "Bearer atoken"
                }
            },
            RequestServices = serviceProvider.Object
        };
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, httpContext);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        authProvider.Verify(ap => ap.GetHandlerAsync(httpContext, JwtBearerDefaults.AuthenticationScheme));
        handler.Verify(h => h.AuthenticateAsync());
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndInvalidApiKey_ThenFails()
    {
        var handler = new Mock<IAuthenticationHandler>();
        handler.Setup(h => h.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.Fail("amessage"));
        var authProvider = new Mock<IAuthenticationHandlerProvider>();
        authProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(handler.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationHandlerProvider)))
            .Returns(authProvider.Object);
        var apiKey = new TokensService().CreateAPIKey().ApiKey;

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HttpConstants.Headers.Authorization] =
                        $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey))}"
                }
            },
            RequestServices = serviceProvider.Object
        };
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, httpContext);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.FailureReasons.Should().OnlyContain(r => r.Message == "amessage");
        authProvider.Verify(ap => ap.GetHandlerAsync(httpContext, APIKeyAuthenticationHandler.AuthenticationScheme));
        handler.Verify(h => h.AuthenticateAsync());
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndValidApiKey_ThenSucceeds()
    {
        var handler = new Mock<IAuthenticationHandler>();
        handler.Setup(h => h.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "ascheme")));
        var authProvider = new Mock<IAuthenticationHandlerProvider>();
        authProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(handler.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationHandlerProvider)))
            .Returns(authProvider.Object);
        var apiKey = new TokensService().CreateAPIKey().ApiKey;

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HttpConstants.Headers.Authorization] =
                        $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey))}"
                }
            },
            RequestServices = serviceProvider.Object
        };
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, httpContext);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        authProvider.Verify(ap => ap.GetHandlerAsync(httpContext, APIKeyAuthenticationHandler.AuthenticationScheme));
        handler.Verify(h => h.AuthenticateAsync());
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndInvalidHmacSignature_ThenFails()
    {
        var handler = new Mock<IAuthenticationHandler>();
        handler.Setup(h => h.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.Fail("amessage"));
        var authProvider = new Mock<IAuthenticationHandlerProvider>();
        authProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(handler.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationHandlerProvider)))
            .Returns(authProvider.Object);
        var signature = new HMACSigner("abody", "asecret").Sign();

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HttpConstants.Headers.HMACSignature] = signature
                }
            },
            RequestServices = serviceProvider.Object
        };
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, httpContext);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.FailureReasons.Should().OnlyContain(r => r.Message == "amessage");
        authProvider.Verify(ap => ap.GetHandlerAsync(httpContext, HMACAuthenticationHandler.AuthenticationScheme));
        handler.Verify(h => h.AuthenticateAsync());
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndValidHMacSignature_ThenSucceeds()
    {
        var handler = new Mock<IAuthenticationHandler>();
        handler.Setup(h => h.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "ascheme")));
        var authProvider = new Mock<IAuthenticationHandlerProvider>();
        authProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(handler.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationHandlerProvider)))
            .Returns(authProvider.Object);
        var signature = new HMACSigner("abody", "asecret").Sign();

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HttpConstants.Headers.HMACSignature] = signature
                }
            },
            RequestServices = serviceProvider.Object
        };
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, httpContext);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        authProvider.Verify(ap => ap.GetHandlerAsync(httpContext, HMACAuthenticationHandler.AuthenticationScheme));
        handler.Verify(h => h.AuthenticateAsync());
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndInvalidBeffeAuthNCookies_ThenFails()
    {
        var handler = new Mock<IAuthenticationHandler>();
        handler.Setup(h => h.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.Fail("amessage"));
        var authProvider = new Mock<IAuthenticationHandlerProvider>();
        authProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(handler.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationHandlerProvider)))
            .Returns(authProvider.Object);
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: [new Claim(AuthenticationConstants.Claims.ForId, "auserid")]
        ));
        var cookieCollection = new Mock<IRequestCookieCollection>();
        cookieCollection.Setup(c => c.TryGetValue(It.IsAny<string>(), out It.Ref<string?>.IsAny))
            .Returns((string _, ref string? val) =>
            {
                val = new AuthNTokenCookieValue { Token = token }.ToJson();
                return true;
            });

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Cookies = cookieCollection.Object
            },
            RequestServices = serviceProvider.Object
        };
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, httpContext);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.FailureReasons.Should().OnlyContain(r => r.Message == "amessage");
        authProvider.Verify(
            ap => ap.GetHandlerAsync(httpContext, BeffeCookieAuthenticationHandler.AuthenticationScheme));
        handler.Verify(h => h.AuthenticateAsync());
    }

    [Fact]
    public async Task WhenHandleRequirementAsyncAndValidBeffeAuthNCookies_ThenSucceeds()
    {
        var handler = new Mock<IAuthenticationHandler>();
        handler.Setup(h => h.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "ascheme")));
        var authProvider = new Mock<IAuthenticationHandlerProvider>();
        authProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(handler.Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationHandlerProvider)))
            .Returns(authProvider.Object);
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: [new Claim(AuthenticationConstants.Claims.ForId, "auserid")]
        ));
        var cookieCollection = new Mock<IRequestCookieCollection>();
        cookieCollection.Setup(c => c.TryGetValue(It.IsAny<string>(), out It.Ref<string?>.IsAny))
            .Returns((string _, ref string? val) =>
            {
                val = new AuthNTokenCookieValue { Token = token }.ToJson();
                return true;
            });

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Cookies = cookieCollection.Object
            },
            RequestServices = serviceProvider.Object
        };
        var context = new AuthorizationHandlerContext([_requirement],
            ClaimsPrincipal.Current!, httpContext);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        authProvider.Verify(
            ap => ap.GetHandlerAsync(httpContext, BeffeCookieAuthenticationHandler.AuthenticationScheme));
        handler.Verify(h => h.AuthenticateAsync());
    }
}