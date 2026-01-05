using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Interfaces.Clients;
using Polly.Retry;

namespace Infrastructure.Web.Api.Common.Clients;

/// <summary>
///     A service client used to call external 3rd party services, with retries
/// </summary>
[ExcludeFromCodeCoverage]
public class ApiServiceClient : IServiceClient
{
    private const int RetryCount = 1;
    protected readonly string BaseUrl;
    protected readonly IHttpClientFactory ClientFactory;
    protected readonly JsonSerializerOptions JsonOptions;
    private readonly AsyncRetryPolicy _retryPolicy;

    public ApiServiceClient(IHttpClientFactory clientFactory, JsonSerializerOptions jsonOptions, string baseUrl) : this(
        clientFactory, jsonOptions, baseUrl, RetryCount)
    {
    }

    protected ApiServiceClient(IHttpClientFactory clientFactory, JsonSerializerOptions jsonOptions, string baseUrl,
        int retryCount)
    {
        ClientFactory = clientFactory;
        JsonOptions = jsonOptions;
        BaseUrl = baseUrl;
        _retryPolicy = ApiClientRetryPolicies.CreateRetryWithExponentialBackoffAndJitter(retryCount);
    }

    public async Task<Result<string?, ResponseProblem>> DeleteAsync(ICallerContext? caller,
        IWebRequest request, Action<HttpRequestMessage>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.DeleteAsync(request, modifiedRequestInterceptor, modifiedResponseInterceptor, ct))
                .Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task FireAsync(ICallerContext? caller, IWebRequestVoid request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        await _retryPolicy.ExecuteAsync(
            async ct => await client.SendOneWayAsync(request, modifiedRequestInterceptor, modifiedResponseInterceptor,
                ct),
            cancellationToken ?? CancellationToken.None);
    }

    public async Task FireAsync<TResponse>(ICallerContext? caller, IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestInterceptor, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        await _retryPolicy.ExecuteAsync(
            async ct =>
            {
                await client.SendOneWayAsync(request, modifiedRequestInterceptor, modifiedResponseInterceptor, ct);
            },
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> GetAsync<TResponse>(ICallerContext? caller,
        IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.GetAsync(request, modifiedRequestInterceptor, modifiedResponseInterceptor, ct))
                .Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<BinaryResponse, ResponseProblem>> GetBinaryAsync(ICallerContext? caller,
        IWebRequest request, Action<HttpRequestMessage>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        return await _retryPolicy.ExecuteAsync(
            async ct =>
            {
                var response =
                    await client.GetAsync(request, modifiedRequestInterceptor, modifiedResponseInterceptor, ct);
                return new BinaryResponse
                {
                    Content = response.RawContent!,
                    ContentType = response.ContentHeaders.ContentType?.MediaType!,
                    ContentLength = response.ContentHeaders.ContentLength!.Value,
                    StatusCode = response.StatusCode
                };
            },
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<string?, ResponseProblem>> GetStringAsync(ICallerContext? caller, IWebRequest request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.GetAsync(request, modifiedRequestInterceptor, modifiedResponseInterceptor, ct))
                .Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PatchAsync<TResponse>(ICallerContext? caller,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PatchAsync(request, modifiedRequestInterceptor, modifiedResponseInterceptor, ct))
                .Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PostAsync<TResponse>(ICallerContext? caller,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PostAsync(request, modifiedRequestInterceptor, modifiedResponseInterceptor, ct))
                .Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PostAsync<TResponse>(ICallerContext? caller,
        IWebRequest<TResponse> request, PostFile file, Action<HttpRequestMessage>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PostAsync(request, file, modifiedRequestInterceptor, modifiedResponseInterceptor,
                ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PutAsync<TResponse>(ICallerContext? caller,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestInterceptor = null,
        Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        using var client = CreateJsonClient(caller, requestInterceptor, responseInterceptor,
            out var modifiedRequestInterceptor, out var modifiedResponseInterceptor);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PutAsync(request, modifiedRequestInterceptor, modifiedResponseInterceptor, ct))
                .Content,
            cancellationToken ?? CancellationToken.None);
    }

    protected virtual JsonClient CreateJsonClient(ICallerContext? caller,
        Action<HttpRequestMessage>? inboundRequestInterceptor, Action<HttpResponseMessage>? inboundResponseInterceptor,
        out Action<HttpRequestMessage> modifiedRequestInterceptor,
        out Action<HttpResponseMessage> modifiedResponseInterceptor)
    {
        var client = new JsonClient(ClientFactory, JsonOptions);
        client.SetBaseUrl(BaseUrl);
        modifiedRequestInterceptor = inboundRequestInterceptor.Exists()
            ? inboundRequestInterceptor
            : _ => { };
        modifiedResponseInterceptor = inboundResponseInterceptor.Exists()
            ? inboundResponseInterceptor
            : _ => { };

        return client;
    }
}