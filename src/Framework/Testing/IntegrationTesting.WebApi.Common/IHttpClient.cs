using System.Net;

namespace IntegrationTesting.WebApi.Common;

/// <summary>
///     Defines a HTTP client
/// </summary>
public interface IHttpClient
{
    Uri? BaseAddress { get; }

    Task<HttpResponseMessage> GetAsync(string route,
        Action<HttpRequestMessage, CookieContainer>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null);

    Task<string> GetStringAsync(string route, Action<HttpRequestMessage, CookieContainer>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null);

    Task<HttpResponseMessage> PostAsync(string route, HttpContent content,
        Action<HttpRequestMessage, CookieContainer>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null);

    Task<HttpResponseMessage> PostEmptyJsonAsync(string route,
        Action<HttpRequestMessage, CookieContainer>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null);

    Task<HttpResponseMessage> SendAsync(HttpRequestMessage message,
        Action<HttpRequestMessage, CookieContainer>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null);
}