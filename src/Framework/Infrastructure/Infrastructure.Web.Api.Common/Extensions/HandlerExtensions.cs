using System.Diagnostics.CodeAnalysis;
using System.Net;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Common.Extensions;

[ExcludeFromCodeCoverage]
public static class HandlerExtensions
{
    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult(this ApiStreamResult result, OperationMethod method)
    {
        return result()
            .Match(response => response.Value.ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult(this ApiEmptyResult result, OperationMethod method)
    {
        return result()
            .Match(response => (response.HasValue
                    ? response.Value
                    : new PostResult<EmptyResponse>(new EmptyResponse())).ToResult(method),
                error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebResponse
    {
        return result()
            .Match(response => ((PostResult<TResponse>)response.Value).ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiPostResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebResponse
    {
        return result()
            .Match(response => response.Value.ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiRedirectResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebResponse
    {
        return result()
            .Match(response => response.Value.ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiPutPatchResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebResponse
    {
        return result()
            .Match(response => ((PostResult<TResponse>)response.Value).ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiGetResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebResponse
    {
        return result()
            .Match(response => ((PostResult<TResponse>)response.Value).ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult<TResource, TResponse>(this ApiSearchResult<TResource, TResponse> result,
        OperationMethod method)
        where TResponse : IWebSearchResponse
    {
        return result()
            .Match(response => ((PostResult<TResponse>)response.Value).ToResult(method), error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="result" /> into an appropriate <see cref="IResult" /> depending on error returned
    /// </summary>
    public static IResult HandleApiResult(this ApiDeleteResult result, OperationMethod method)
    {
        return result()
            .Match(response => ((PostResult<EmptyResponse>)response.Value).ToResult(method),
                error => error.ToResult());
    }

    /// <summary>
    ///     Converts the <see cref="Result{TResource,Error}" /> to an <see cref="Result{EmptyResponse,Error}" />
    /// </summary>
    public static Result<EmptyResponse, Error> HandleApplicationResult<TResource>(
        this Result<TResource, Error> resource)
    {
        return resource.Match(_ => new Result<EmptyResponse, Error>(new EmptyResponse()),
            error => new Result<EmptyResponse, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="TResource" /> in the <see cref="Result{TResource,Error}" /> to an
    ///     <see cref="Result{TResponse,Error}" />
    ///     using the <see cref="onSuccess" /> callback
    /// </summary>
    public static Result<TResponse, Error> HandleApplicationResult<TResource, TResponse>(
        this Result<TResource, Error> resource, Func<TResource, TResponse> onSuccess)
        where TResponse : IWebResponse
    {
        return resource.Match(res => new Result<TResponse, Error>(onSuccess(res.Value)),
            error => new Result<TResponse, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="TResource" /> in the <see cref="Result{TResource,Error}" /> to an
    ///     <see cref="RedirectResult{TResponse,Error}" />
    ///     using the <see cref="onSuccess" /> callback
    /// </summary>
    public static Result<RedirectResult<TResponse>, Error> HandleApplicationResult<TResource, TResponse>(
        this Result<TResource, Error> resource, Func<TResource, TResponse?> onSuccess, string? redirectUri)
        where TResponse : IWebResponse
    {
        return resource.Match(
            res => new Result<RedirectResult<TResponse>, Error>(
                new RedirectResult<TResponse>(onSuccess(res.Value), redirectUri)),
            error => new Result<RedirectResult<TResponse>, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="TResource" /> in the <see cref="Result{TResource,Error}" /> to an
    ///     <see cref="RedirectResult{TResponse,Error}" />
    ///     using the <see cref="onSuccess" /> callback
    /// </summary>
    public static Result<RedirectResult<TResponse>, Error> HandleApplicationResult<TResource, TResponse>(
        this Result<TResource, Error> resource, Func<TResource, RedirectResult<TResponse>> onSuccess)
        where TResponse : IWebResponse
    {
        return resource.Match(res => new Result<RedirectResult<TResponse>, Error>(onSuccess(res.Value)),
            error => new Result<RedirectResult<TResponse>, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="TResource" /> in the <see cref="Result{TResource,Error}" /> to an
    ///     <see cref="Result{PostResult,Error}" />
    ///     using the <see cref="onSuccess" /> callback
    /// </summary>
    public static Result<PostResult<TResponse>, Error> HandleApplicationResult<TResource, TResponse>(
        this Result<TResource, Error> resource, Func<TResource, PostResult<TResponse>> onSuccess)
        where TResponse : IWebResponse
    {
        return resource.Match(res => new Result<PostResult<TResponse>, Error>(onSuccess(res.Value)),
            error => new Result<PostResult<TResponse>, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="Result{Error}" /> to an <see cref="Result{EmptyResponse,Error}" />
    /// </summary>
    public static Result<EmptyResponse, Error> HandleApplicationResult(this Result<Error> resource)
    {
        return resource.Match(() => new Result<EmptyResponse, Error>(new EmptyResponse()),
            error => new Result<EmptyResponse, Error>(error));
    }

    /// <summary>
    ///     Converts the <see cref="Result{IHasStream,Error}" /> to an <see cref="Result{StreamResult,Error}" />
    /// </summary>
    public static Result<StreamResult, Error> HandleApplicationResult<TResource>(this Result<TResource, Error> resource,
        Func<TResource, StreamResult> onSuccess)
    {
        return resource.Match(res => new Result<StreamResult, Error>(onSuccess(res.Value)),
            error => new Result<StreamResult, Error>(error));
    }

    private static IResult ToResult(this StreamResult result, OperationMethod _)
    {
        return Results.Stream(result.Stream, result.ContentType, result.FileName);
    }

    private static IResult ToResult<TResponse>(this PostResult<TResponse> postResult, OperationMethod method)
        where TResponse : IWebResponse
    {
        var response = postResult.Response;
        var location = postResult.Location;

        return ToResult(response, location, method);
    }

    private static IResult ToResult<TResponse>(this RedirectResult<TResponse> redirectResult, OperationMethod method)
        where TResponse : IWebResponse
    {
        var response = redirectResult.Response;
        var redirect = redirectResult.RedirectUri;

        if (redirect.HasValue())
        {
            return Results.Redirect(redirect);
        }

        return ToResult(response, null, method);
    }

    private static IResult ToResult<TResponse>(TResponse? response, string? location, OperationMethod method)
        where TResponse : IWebResponse
    {
        var options =
            new ResponseCodeOptions(response is not EmptyResponse, location.HasValue());
        var statusCode = method.ToHttpMethod().GetDefaultResponseCode(options);

        return statusCode switch
        {
            HttpStatusCode.OK => Results.Ok(response),
            HttpStatusCode.Accepted => Results.Accepted(null, response),
            HttpStatusCode.Created => Results.Created(location, response),
            HttpStatusCode.NoContent => Results.NoContent(),
            _ => Results.Ok(response)
        };
    }

    private static IResult ToResult(this Error error)
    {
        return error.ToProblem();
    }
}