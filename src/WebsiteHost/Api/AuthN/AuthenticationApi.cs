using Application.Resources.Shared;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using Infrastructure.Web.Hosting.Common.Extensions;
using WebsiteHost.Application;

namespace WebsiteHost.Api.AuthN;

[BaseApiFrom("/api")]
public sealed class AuthenticationApi : IWebApiService
{
    private readonly IAuthenticationApplication _authenticationApplication;
    private readonly ICallerContextFactory _callerFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationApi(ICallerContextFactory callerFactory, IAuthenticationApplication authenticationApplication,
        IHttpContextAccessor httpContextAccessor)
    {
        _callerFactory = callerFactory;
        _authenticationApplication = authenticationApplication;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApiPostResult<AuthenticateTokens, AuthenticateResponse>> Authenticate(
        AuthenticateRequest request, CancellationToken cancellationToken)
    {
        var tokens = await _authenticationApplication.AuthenticateAsync(_callerFactory.Create(), request.Provider!,
            request.AuthCode, request.Username, request.Password, cancellationToken);
        if (tokens.IsSuccessful)
        {
            var response = _httpContextAccessor.HttpContext!.Response;
            response.SetTokensToAuthNCookies(tokens.Value);
        }

        return () => tokens.HandleApplicationResult<AuthenticateTokens, AuthenticateResponse>(tok =>
            new PostResult<AuthenticateResponse>(new AuthenticateResponse { UserId = tok.UserId }));
    }

    public async Task<ApiEmptyResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _authenticationApplication.LogoutAsync(_callerFactory.Create(), cancellationToken);
        if (result.IsSuccessful)
        {
            var response = _httpContextAccessor.HttpContext!.Response;
            response.DeleteAuthNCookies();
        }

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }

    public async Task<ApiEmptyResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = _httpContextAccessor.HttpContext!.Request.GetRefreshTokenFromAuthNCookies();

        var tokens =
            await _authenticationApplication.RefreshTokenAsync(_callerFactory.Create(), refreshToken,
                cancellationToken);
        if (tokens.IsSuccessful)
        {
            var response = _httpContextAccessor.HttpContext!.Response;
            response.SetTokensToAuthNCookies(tokens.Value);
        }

        return () => tokens.Match(_ => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
}