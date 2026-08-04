namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request of a GET API
/// </summary>
public interface IWebGetRequest<TResponse> : IWebRequest<TResponse>, IHasGetOptions
    where TResponse : IWebResponse;