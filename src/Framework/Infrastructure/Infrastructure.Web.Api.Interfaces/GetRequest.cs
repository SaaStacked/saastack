using System.ComponentModel;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request of a GET API
/// </summary>
public abstract class GetRequest<TRequest, TResponse> : WebRequest<TRequest, TResponse>, IWebGetRequest<TResponse>
    where TResponse : IWebResponse
    where TRequest : IWebRequest
{
    [Description("List of child resources to embed in the resource")]
    public string? Embed { get; set; }
}