using System.Net;
using System.Text;
using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Colors = Infrastructure.Common.ConsoleConstants.Colors;

namespace Infrastructure.Web.Api.Common.Endpoints;

/// <summary>
///     Provides a request and response filter that records the request and response outputs.
///     Note: This method may disclose sensitive information contained in headers and bodies of requests,
///     and should only be used for diagnosing purposes TraceLevel.Debug.
///     Note: In TESTINGONLY the trace includes console colors to stand out in local development in the console.
///     There is no trace coloring in production.
///     Note: Since this is implemented as an <see cref="IEndpointFilter" /> we are able to determine
///     the type of response coming from the endpoint, and thus extract the RFC7807 contents easily here.
///     This would not be straight forward (if at all possible without dealing with response streams),
///     if we implemented this as ASPNET middleware.
/// </summary>
public class HttpRecordingFilter : IEndpointFilter
{
    private const int MaxBodySize = 1500;
    private static readonly string[] RequestHeadersOfInterest =
    {
        HttpConstants.Headers.Accept,
        HttpConstants.Headers.ContentType,
        HttpConstants.Headers.Authorization,
        HttpConstants.Headers.HMACSignature,
        HttpConstants.Headers.AntiCSRF,
        HttpConstants.Headers.Tenant,
        HttpConstants.Headers.PrivateInterHostSignature
    };
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IRecorder _recorder;

    public HttpRecordingFilter(IRecorder recorder, ICallerContextFactory callerContextFactory)
    {
        _recorder = recorder;
        _callerContextFactory = callerContextFactory;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var caller = _callerContextFactory.Create();
        var httpRequest = context.HttpContext.Request;

        await TraceRequestAsync(_recorder, caller, httpRequest, context.HttpContext.RequestAborted);

        var response = await next(context);
        if (response is null)
        {
            return response;
        }

        var requestDescriptor = GetSimpleDescriptor(httpRequest);
        TraceResponse(_recorder, caller, context, requestDescriptor, response);
        return response;
    }

    public static string GetSimpleDescriptor(HttpRequest request)
    {
        var path = request.Path;
        var method = request.Method;

        return $"{method} {path}";
    }

    private async Task TraceRequestAsync(IRecorder recorder, ICallerContext caller, HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var queryString = httpRequest.QueryString.HasValue
            ? $" ?{httpRequest.QueryString}"
            : string.Empty;
        var headersValues = httpRequest.Headers
            .Where(pair => RequestHeadersOfInterest.ContainsIgnoreCase(pair.Key))
            .Where(value => value.Value.ToString().HasValue())
            .ToDictionary(pair => pair.Key, pair => pair.Value)
            .Select(pair => $"{pair.Key}={pair.Value.JoinAsOredChoices()}")
            .JoinAsOredChoices();
        var headers = headersValues.HasValue()
            ? $" [{headersValues}]"
            : string.Empty;

        var body = string.Empty;
        if (_recorder.TraceLevel == RecorderTraceLevel.Debug)
        {
            var bodyValue = await GetRequestBodyAsync(httpRequest, cancellationToken);
            body = bodyValue.HasValue()
                ? $" with body: {bodyValue}"
                : string.Empty;
        }

        var requestDescriptor = $"{httpRequest.Method} {httpRequest.Path} Received, with{queryString}{headers}{body}";

        recorder.TraceInformation(caller.ToCall(),
#if TESTINGONLY
            $"{Colors.Blue}{{Request}}{Colors.Normal}",
#else
            "{Request}",
#endif
            requestDescriptor);
    }

    private static void TraceResponse(IRecorder recorder, ICallerContext caller,
        EndpointFilterInvocationContext context, string requestDescriptor, object response)
    {
        var httpResponse = context.HttpContext.Response;
        var statusCode = httpResponse.StatusCode;

        if (response is IStatusCodeHttpResult statusCodeResult)
        {
            statusCode = statusCodeResult.StatusCode ?? statusCode;
        }

        // Log the outgoing response
        if (response is IValueHttpResult { Value: ProblemDetails problemDetails })
        {
            statusCode = problemDetails.Status ?? statusCode;
            var responseBody = problemDetails.ToJson()!;
            RecordErrors(responseBody);
            return;
        }

        RecordSuccess();
        return;

        void RecordSuccess()
        {
            var statusCodeDescription = $"{statusCode} - {(HttpStatusCode)statusCode}";
            recorder.TraceInformation(caller.ToCall(),
#if TESTINGONLY
                $"{Colors.Green}{{Request}}: {{Result}}{Colors.Normal}",
#else
                "{Request}: {Result}",
#endif
                requestDescriptor, statusCodeDescription);
        }

        void RecordErrors(string errorDetails)
        {
            var statusCodeDescription = $"{statusCode} - {(HttpStatusCode)statusCode}";
            switch (statusCode)
            {
                case >= 500:
                    recorder.TraceError(caller.ToCall(),
#if TESTINGONLY
                        $"{Colors.Red}{Colors.Bold}{{Request}}{Colors.NoBold}: {{Result}}{Colors.Normal}, problem: {{Problem}}",
#else
                        "{Request}: {Result}, problem: {Problem}",
#endif
                        requestDescriptor, statusCodeDescription, errorDetails);
                    break;

                case >= 400:
                    recorder.TraceInformation(caller.ToCall(),
#if TESTINGONLY
                        $"{Colors.Yellow}{{Request}}: {{Result}}{Colors.Normal}, problem: {{Problem}}",
#else
                        "{Request}: {Result}, problem: {Problem}",
#endif
                        requestDescriptor, statusCodeDescription, errorDetails);
                    break;

                default:
                    RecordSuccess();
                    break;
            }
        }
    }

    private static async Task<string?> GetRequestBodyAsync(HttpRequest httpRequest, CancellationToken cancellationToken)
    {
        if (!httpRequest.CanHaveBody())
        {
            return null;
        }

        try
        {
            httpRequest.RewindBody();
            var body = await httpRequest.Body.ReadFullyAsync(cancellationToken);
            if (body.Exists())
            {
                if (body.Length == 0)
                {
                    return "(empty)";
                }

                var content = Encoding.UTF8.GetString(body);

                var sanitizedBody = (content.Length > MaxBodySize
                        ? content[..MaxBodySize] + "..."
                        : content)
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r"); //we want to see the newlines in the log, but not have newlines in the log
                return sanitizedBody;
            }

            return null;
        }
        finally
        {
            httpRequest.RewindBody();
        }
    }
}