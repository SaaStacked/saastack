using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace WebsiteHost.Application;

public class AuthenticationApplication : IAuthenticationApplication
{
    private readonly IRecorder _recorder;
    private readonly IServiceClient _serviceClient;

    public AuthenticationApplication(IRecorder recorder, IServiceClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
    }

    public async Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller, string provider,
        string? authCode, string? username, string? password, CancellationToken cancellationToken)
    {
        Task<Result<AuthenticateResponse, ResponseProblem>> request;
        switch (provider)
        {
            case AuthenticationConstants.Providers.Credentials:
                request = _serviceClient.PostAsync(caller, new AuthenticateCredentialRequest
                {
                    Username = username!,
                    Password = password!
                }, req => req.RemoveAuthorization(), cancellationToken);
                break;

            default:
                request = _serviceClient.PostAsync(caller, new AuthenticateSingleSignOnRequest
                {
                    AuthCode = authCode!,
                    Provider = provider
                }, req => req.RemoveAuthorization(), cancellationToken);
                break;
        }

        var authenticated = await request;
        if (authenticated.IsFailure)
        {
            return authenticated.Error.ToError();
        }

        return authenticated.Value.ToTokens();
    }

    public Task<Result<Error>> LogoutAsync(ICallerContext caller, CancellationToken cancellationToken)
    {
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.UserLogout);

        return Task.FromResult(Result.Ok);
    }

    public async Task<Result<AuthenticateTokens, Error>> RefreshTokenAsync(ICallerContext caller, string? refreshToken,
        CancellationToken cancellationToken)
    {
        if (!refreshToken.HasValue())
        {
            return Error.NotAuthenticated();
        }

        var refreshed = await _serviceClient.PostAsync(caller, new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        }, req => req.RemoveAuthorization(), cancellationToken);
        if (refreshed.IsFailure)
        {
            return refreshed.Error.ToError();
        }

        return refreshed.Value.ToTokens();
    }
}

internal static class AuthenticationConversionExtensions
{
    public static AuthenticateTokens ToTokens(this AuthenticateResponse response)
    {
        return response.Tokens;
    }

    public static AuthenticateTokens ToTokens(this RefreshTokenResponse response)
    {
        return response.Tokens;
    }
}