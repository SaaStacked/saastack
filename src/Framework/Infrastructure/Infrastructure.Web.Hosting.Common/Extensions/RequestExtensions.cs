using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Web.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common.Extensions;


public static class RequestExtensions
{
    private const int MaxCookiePayloadSizeInBytes = 4096;

    /// <summary>
    ///     Whether any authorization method has been provided in the request
    /// </summary>
    public static bool IsAnyAuthorizationProvided(this HttpRequest request)
    {
        return request.GetTokenAuth().HasValue
               || request.GetAPIKeyAuth().HasValue
               || request.GetBasicAuth().Username.HasValue
               || request.GetHMACAuth().HasValue
               || request.GetPrivateInterHostAuth().HasValue
               || request.GetTokenFromAuthNCookies().HasValue;
    }
    
    /// <summary>
    ///     Deletes the authentication cookies from the response
    /// </summary>
    public static void DeleteAuthNCookies(this HttpResponse response)
    {
        response.Cookies.Delete(AuthenticationConstants.Cookies.Token);
        response.Cookies.Delete(AuthenticationConstants.Cookies.RefreshToken);
    }

    /// <summary>
    ///     Returns the auth token from the specified cookie value
    /// </summary>
    public static Optional<AuthNTokenCookieValue> GetAuthCookieValue(string cookieValue)
    {
        //Note: IResponseCookies.Append automatically Uri.escapes the JSON cookie value
        var decoded = Uri.UnescapeDataString(cookieValue);
        return decoded.FromJson<AuthNTokenCookieValue>();
    }

    /// <summary>
    ///     Returns the claims from the JWT token that is stored in the AuthN cookie,
    /// </summary>
    public static Optional<Claim[]> GetAuthNCookie(this HttpRequest request)
    {
        var token = GetTokenFromAuthNCookies(request);
        if (!token.HasValue)
        {
            return Optional<Claim[]>.None;
        }

        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);
            return jwtToken.Claims.ToArray().ToOptional();
        }
        catch (Exception)
        {
            return Optional<Claim[]>.None;
        }
    }

    /// <summary>
    ///     Returns the refresh token from the authentication cookies.
    ///     Note: If the cookie has expired then cookie no longer exists, and this will return None.
    /// </summary>
    public static Optional<string> GetRefreshTokenFromAuthNCookies(this HttpRequest request)
    {
        if (request.Cookies.TryGetValue(AuthenticationConstants.Cookies.RefreshToken, out var value))
        {
            var cookieValue = GetAuthCookieValue(value);
            return cookieValue.Value.Token;
        }

        return Optional<string>.None;
    }

    /// <summary>
    ///     Returns the JWT token from the authentication cookies.
    ///     Note: If the cookie has expired then cookie no longer exists, and this will return None.
    /// </summary>
    public static Optional<string> GetTokenFromAuthNCookies(this HttpRequest request)
    {
        if (request.Cookies.TryGetValue(AuthenticationConstants.Cookies.Token, out var value))
        {
            var cookieValue = GetAuthCookieValue(value);
            return cookieValue.Value.Token;
        }

        return Optional<string>.None;
    }

    /// <summary>
    ///     Returns the ID of the user from the JWT token that is stored in the cookie,
    ///     while the cookie has not expired
    /// </summary>
    public static Result<Optional<string>, Error> GetUserIdFromAuthNCookie(this HttpRequest request)
    {
        var token = GetTokenFromAuthNCookies(request);
        if (!token.HasValue)
        {
            return Optional<string>.None;
        }

        var userId = GetUserIdClaim(token);
        if (!userId.HasValue)
        {
            return Error.ForbiddenAccess(Resources.RequestExtensions_InvalidToken);
        }

        return userId.Value.ToOptional();
    }

    /// <summary>
    ///     Populates the authentication session cookies for the client.
    ///     We set the expiry of both cookies to the expiry of the refresh token.
    ///     Typical expiry times for the auth token would be relatively short, see:
    ///     <see cref="AuthenticationConstants.Tokens.DefaultAccessTokenExpiry" />
    ///     Whereas the refresh token would be much longer:
    ///     <see cref="AuthenticationConstants.Tokens.DefaultRefreshTokenExpiry" />
    ///     We set the expiry of the auth cookie and refresh cookie to the expiry of the refresh token,
    ///     so that both cookies are valid for the duration of the 'refreshable session',
    ///     instead of only being available for the lifetime of the JWT token.
    ///     We expect that when this cookie is unpacked and the embedded JWT is sent to the backend API, it will be validated
    ///     and rejected if the JWT is expired, and this will force the client to refresh the token.
    /// </summary>
    public static void SetTokensToAuthNCookies(this HttpResponse response, AuthenticateTokens tokens)
    {
        var expiresOn = tokens.RefreshToken.ExpiresOn;
        var authToken = new AuthNTokenCookieValue
        {
            Token = tokens.AccessToken.Value,
            ExpiresOn = expiresOn
        }.ToJson()!;
        var refreshToken = new AuthNTokenCookieValue
        {
            Token = tokens.RefreshToken.Value,
            ExpiresOn = expiresOn
        }.ToJson()!;

        var tokenLength = authToken.Length;
        if (tokenLength > MaxCookiePayloadSizeInBytes)
        {
            var overSize = tokenLength - MaxCookiePayloadSizeInBytes;
            throw new InvalidOperationException(
                Resources.RequestExtensions_SetTokensToAuthNCookies_TokenLengthExceeded.Format(overSize));
        }

        // Note: IResponseCookies.Append will automatically call Uri.EscapeDataString on the JSON value
        response.Cookies.Append(AuthenticationConstants.Cookies.Token, authToken,
            GetCookieOptions(expiresOn));
        response.Cookies.Append(AuthenticationConstants.Cookies.RefreshToken, refreshToken,
            GetCookieOptions(expiresOn));
    }

    private static Optional<string> GetUserIdClaim(string token)
    {
        try
        {
            var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToArray();
            var userClaim = claims
                .FirstOrDefault(claim => claim.Type == AuthenticationConstants.Claims.ForId);
            if (userClaim.NotExists())
            {
                return Optional<string>.None;
            }

            return userClaim.Value.ToOptional();
        }
        catch (Exception)
        {
            return Optional<string>.None;
        }
    }

    /// <summary>
    ///     Returns the cookie options for the cookie
    ///     It is imperative that this cookie is not accessible to JavaScript, and is sent only over HTTPS.
    /// </summary>
    private static CookieOptions GetCookieOptions(DateTime? expires)
    {
        var options = new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expires.HasValue
                ? new DateTimeOffset(expires.Value)
                : null,
            MaxAge = expires.HasValue
                ? expires.Value.Subtract(DateTime.UtcNow)
                : null
        };

        return options;
    }
}