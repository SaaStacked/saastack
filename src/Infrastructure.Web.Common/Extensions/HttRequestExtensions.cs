using System.Net.Http.Headers;
using System.Text;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Validations;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Common.Extensions;

public static class HttRequestExtensions
{
    /// <summary>
    ///     Whether the specified <see cref="method" /> could have a content body.
    /// </summary>
    public static bool CanHaveBody(this HttpMethod method)
    {
        return method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch;
    }

    /// <summary>
    ///     Whether the specified <see cref="request" /> could have a content body.
    /// </summary>
    public static bool CanHaveBody(this HttpRequest request)
    {
        var httpMethod = new HttpMethod(request.Method);
        return httpMethod.CanHaveBody();
    }

    /// <summary>
    ///     Returns the value of the APIKEY authorization from the request (if any)
    /// </summary>
    public static Optional<string> GetAPIKeyAuth(this HttpRequest request)
    {
        var fromQuery = request.Query[HttpConstants.QueryParams.APIKey].FirstOrDefault();
        if (fromQuery.HasValue())
        {
            var parameter = fromQuery;
            if (CommonValidations.APIKeys.ApiKey.Matches(parameter))
            {
                return parameter;
            }

            return Optional<string>.None;
        }

        var fromBasicAuth = GetBasicAuth(request);
        if (!fromBasicAuth.Username.HasValue)
        {
            return Optional<string>.None;
        }

        if (fromBasicAuth.Username.HasValue
            && !fromBasicAuth.Password.HasValue)
        {
            var username = fromBasicAuth.Username;
            if (CommonValidations.APIKeys.ApiKey.Matches(username))
            {
                return username;
            }

            return Optional<string>.None;
        }

        return Optional<string>.None;
    }

    /// <summary>
    ///     Returns the values of the BASIC authentication from the request (if any)
    /// </summary>
    public static (Optional<string> Username, Optional<string> Password) GetBasicAuth(this HttpRequest request)
    {
        var fromBasicAuth = AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var result)
            ? result
            : null;
        if (fromBasicAuth.NotExists())
        {
            return (Optional<string>.None, Optional<string>.None);
        }

        var token = result!.Parameter;
        if (token.HasNoValue())
        {
            return (Optional<string>.None, Optional<string>.None);
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var delimiterIndex = decoded.IndexOf(':', StringComparison.Ordinal);
            if (delimiterIndex == -1)
            {
                return (decoded, Optional<string>.None);
            }

            var username = decoded.Substring(0, delimiterIndex);
            var password = decoded.Substring(delimiterIndex + 1);
            return (username.HasValue()
                    ? username
                    : Optional<string>.None,
                password.HasValue()
                    ? password
                    : Optional<string>.None);
        }
        catch (FormatException)
        {
            return (Optional<string>.None, Optional<string>.None);
        }
        catch (IndexOutOfRangeException)
        {
            return (Optional<string>.None, Optional<string>.None);
        }
    }

    /// <summary>
    ///     Returns the value of the HMAC signature authorization from the request (if any)
    /// </summary>
    public static Optional<string> GetHMACAuth(this HttpRequest request)
    {
        var authorization = request.Headers[HttpConstants.Headers.HMACSignature];
        if (authorization.NotExists() || authorization.Count == 0)
        {
            return Optional<string>.None;
        }

        var signature = authorization.FirstOrDefault();
        if (signature.HasNoValue())
        {
            return Optional<string>.None;
        }

        return signature;
    }

    /// <summary>
    ///     Returns the value of the PrivateInterHost signature authorization from the request (if any)
    /// </summary>
    public static Optional<string> GetPrivateInterHostAuth(this HttpRequest request)
    {
        var authorization = request.Headers[HttpConstants.Headers.PrivateInterHostSignature];
        if (authorization.NotExists() || authorization.Count == 0)
        {
            return Optional<string>.None;
        }

        var signature = authorization.FirstOrDefault();
        if (signature.HasNoValue())
        {
            return Optional<string>.None;
        }

        return signature;
    }

    /// <summary>
    ///     Returns the value of the Bearer token of the JWT authorization from the request (if any)
    /// </summary>
    public static Optional<string> GetTokenAuth(this HttpRequest request)
    {
        var authorization = request.Headers.Authorization;
        if (authorization.NotExists() || authorization.Count == 0)
        {
            return Optional<string>.None;
        }

        var value = authorization.FirstOrDefault(val =>
            val.HasValue() && val.StartsWith(OAuth2Constants.TokenTypes.Bearer));
        if (value.HasNoValue())
        {
            return Optional<string>.None;
        }

        var indexOfToken = OAuth2Constants.TokenTypes.Bearer.Length + 1;
        var token = value.Substring(indexOfToken);

        return token.HasValue()
            ? token
            : Optional<string>.None;
    }

    /// <summary>
    ///     Whether the MediaType (of the ContentType) is the specified <see cref="contentType" />
    /// </summary>
    public static bool IsContentType(this HttpRequest request, string contentType)
    {
        return contentType.IsMediaType(request.ContentType);
    }

    /// <summary>
    ///     Rewinds the <see cref="HttpRequest.Body" /> back to the start
    /// </summary>
    public static void RewindBody(this HttpRequest httpRequest)
    {
        if (httpRequest.Body.CanSeek)
        {
            httpRequest.Body.Rewind();
        }
    }

    /// <summary>
    ///     Sets the <see cref="HttpConstants.Headers.Authorization" /> header of the specified <see cref="message" />
    ///     to the <see cref="apiKey" />
    /// </summary>
    public static void SetAPIKey(this HttpRequestMessage message, string apiKey)
    {
        if (apiKey.HasNoValue())
        {
            return;
        }

        message.SetBasicAuth(apiKey);
    }

    /// <summary>
    ///     Sets the <see cref="ICallerContext.Authorization" /> to Basic with <see cref="username" />, and
    ///     <see cref="password" />
    /// </summary>
    public static void SetBasicAuth(this HttpRequestMessage message, string username, string? password = null)
    {
        if (username.HasNoValue())
        {
            return;
        }

        message.Headers.Add(HttpConstants.Headers.Authorization,
            $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{(password.HasValue() ? password : string.Empty)}"))}");
    }

    /// <summary>
    ///     Sets the <see cref="HttpConstants.Headers.Authorization" /> header of the specified <see cref="message" />
    ///     to the <see cref="token" />
    /// </summary>
    public static void SetJWTBearerToken(this HttpRequestMessage message, string token)
    {
        if (token.HasNoValue())
        {
            return;
        }

        message.Headers.Add(HttpConstants.Headers.Authorization, $"{OAuth2Constants.TokenTypes.Bearer} {token}");
    }

    /// <summary>
    ///     Sets the <see cref="HttpConstants.Headers.RequestId" /> header of the specified <see cref="message" />
    ///     to the <see cref="ICallContext.CallId" />
    /// </summary>
    public static void SetRequestId(this HttpRequestMessage message, ICallContext context)
    {
        var callId = context.CallId;
        if (callId.HasNoValue())
        {
            return;
        }

        if (message.Headers.Contains(HttpConstants.Headers.RequestId))
        {
            return;
        }

        message.Headers.Add(HttpConstants.Headers.RequestId, context.CallId);
    }

    /// <summary>
    ///     Returns a convenient representation of the request
    /// </summary>
    public static string ToDisplayName(this HttpRequest request)
    {
        var path = request.Path;
        var method = request.Method;
        var accept = request.Headers.Accept.ToString();

        return $"{method} {path} ({accept})";
    }

    private static bool IsMediaType(this string? source, string? target)
    {
        if (source.HasNoValue() || target.HasNoValue())
        {
            return false;
        }

        if (!MediaTypeHeaderValue.TryParse(source, out var sourceMediaType))
        {
            return false;
        }

        if (!MediaTypeHeaderValue.TryParse(target, out var targetMediaType))
        {
            return false;
        }

        return sourceMediaType.MediaType.EqualsIgnoreCase(targetMediaType.MediaType);
    }
}