using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Application.Common.Extensions;
using Application.Interfaces;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces;

namespace Infrastructure.Web.Api.Common.Clients;

/// <summary>
///     A service client used to call between API hosts, with retries.
///     Adds both the <see cref="HttpConstants.Headers.RequestId" /> and <see cref="HttpConstants.Headers.Authorization" />
///     to all downstream requests
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InterHostServiceClient : ApiServiceClient
{
    private const int RetryCount = 2;
    private readonly string _privateInterHostSecret;
    private readonly string _hmacSecret;

    public InterHostServiceClient(IHttpClientFactory clientFactory, JsonSerializerOptions jsonOptions, string baseUrl,
        string privateInterHostSecret, string hmacSecret) :
        base(clientFactory, jsonOptions, baseUrl, RetryCount)
    {
        _privateInterHostSecret = privateInterHostSecret;
        _hmacSecret = hmacSecret;
    }

    internal static void SetAuthorization(HttpRequestMessage message, ICallerContext caller,
        string privateInterHostSecret, string hmacSecret)
    {
        var authorization = caller.Authorization;
        if (!authorization.HasValue)
        {
            return;
        }

        var authorizationValue = authorization is { HasValue: true, Value.Value.HasValue: true }
            ? authorization.Value.Value.Value
            : null;

        switch (authorization.Value.Method)
        {
            case ICallerContext.AuthorizationMethod.Token:
            {
                if (authorizationValue.HasValue())
                {
                    var token = authorization.Value.Value.Value;
                    message.SetJWTBearerToken(token);
                }

                break;
            }

            case ICallerContext.AuthorizationMethod.APIKey:
            {
                if (authorizationValue.HasValue())
                {
                    var apiKey = authorization.Value.Value.Value;
                    message.SetAPIKey(apiKey);
                }

                break;
            }

            case ICallerContext.AuthorizationMethod.PrivateInterHost:
            {
                if (authorizationValue.HasValue())
                {
                    var token = authorization.Value.Value.Value;
                    message.SetPrivateInterHostAuth(privateInterHostSecret, token);
                }
                else
                {
                    message.SetPrivateInterHostAuth(privateInterHostSecret);
                }

                break;
            }

            case ICallerContext.AuthorizationMethod.HMAC:
            {
                if (authorizationValue.HasValue())
                {
                    var hmacSecret2 = authorization.Value.Value.Value;
                    message.SetHMACAuth(hmacSecret2);
                }
                else
                {
                    message.SetHMACAuth(hmacSecret);
                }

                break;
            }

            case ICallerContext.AuthorizationMethod.AuthNCookie:
            {
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override JsonClient CreateJsonClient(ICallerContext? caller,
        Action<HttpRequestMessage>? inboundRequestInterceptor, Action<HttpResponseMessage>? inboundResponseInterceptor,
        out Action<HttpRequestMessage> modifiedRequestInterceptor,
        out Action<HttpResponseMessage> modifiedResponseInterceptor)
    {
        var client = new JsonClient(ClientFactory, JsonOptions);
        client.SetBaseUrl(BaseUrl);
        if (inboundRequestInterceptor.Exists())
        {
            modifiedRequestInterceptor = msg =>
            {
                AddCorrelationId(msg, caller);
                AddCallerAuthorization(msg, caller, _privateInterHostSecret, _hmacSecret);
                inboundRequestInterceptor(msg);
            };
        }
        else
        {
            modifiedRequestInterceptor = msg =>
            {
                AddCorrelationId(msg, caller);
                AddCallerAuthorization(msg, caller, _privateInterHostSecret, _hmacSecret);
            };
        }

        modifiedResponseInterceptor = inboundResponseInterceptor.Exists()
            ? inboundResponseInterceptor
            : _ => { };

        return client;
    }

    private static void AddCorrelationId(HttpRequestMessage message, ICallerContext? caller)
    {
        if (caller.Exists())
        {
            message.SetRequestId(caller.ToCall());
        }
    }

    private static void AddCallerAuthorization(HttpRequestMessage message, ICallerContext? caller,
        string privateInterHostSecret, string hmacSecret)
    {
        if (caller.Exists())
        {
            SetAuthorization(message, caller, privateInterHostSecret, hmacSecret);
        }
    }
}