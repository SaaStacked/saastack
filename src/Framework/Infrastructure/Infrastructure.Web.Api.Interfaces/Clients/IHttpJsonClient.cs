namespace Infrastructure.Web.Api.Interfaces.Clients;

/// <summary>
///     Defines a JSON <see cref="HttpClient" />
/// </summary>
public interface IHttpJsonClient
{
    Task<JsonResponse<TResponse>> DeleteAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<JsonResponse> DeleteAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null);

    Task<JsonResponse<TResponse>> GetAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<JsonResponse> GetAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null);

    Task<JsonResponse<TResponse>> PatchAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<JsonResponse> PatchAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null);

    Task<JsonResponse<TResponse>> PostAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<JsonResponse> PostAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null);

    Task<JsonResponse<TResponse>> PostAsync<TResponse>(IWebRequest<TResponse> request, PostFile file,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<JsonResponse> PostAsync(IWebRequest request, PostFile file,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null);

    Task<JsonResponse<TResponse>> PutAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<JsonResponse<TResponse>> PutAsync<TResponse>(IWebRequest<TResponse> request, PostFile file,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<JsonResponse> PutAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestInterceptor = null, Action<HttpResponseMessage>? responseInterceptor = null,
        CancellationToken? cancellationToken = null);
}

/// <summary>
///     Defines a file that has been POSTed to an endpoint
/// </summary>
public record PostFile(Stream Stream, string ContentType, string PartName = "file", string? Filename = null);