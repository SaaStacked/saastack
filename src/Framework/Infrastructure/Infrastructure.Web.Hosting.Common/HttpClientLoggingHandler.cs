using Application.Common.Extensions;
using Common;
using Common.Extensions;
using Infrastructure.Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Handles the logging of all outbound requests from an instance of <see cref="HttpClient" />
///     Note: This method may disclose sensitive information contained in headers and bodies of requests,
///     and should only be used for diagnosing purposes TraceLevel.Debug.
///     Note: In TESTINGONLY the trace includes console colors to stand out in local development in the console.
///     There is no trace coloring in production.
/// </summary>
public class HttpClientLoggingHandler : DelegatingHandler
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

    public HttpClientLoggingHandler(IRecorder recorder, ICallerContextFactory callerContextFactory)
    {
        _recorder = recorder;
        _callerContextFactory = callerContextFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var caller = _callerContextFactory.Create();
        var requestDescriptor = await FormatRequestAsync(request, cancellationToken);

        _recorder.TraceInformation(caller.ToCall(),
#if TESTINGONLY
            $"Outgoing client request: {ConsoleConstants.Colors.Cyan}{{Details}}{ConsoleConstants.Colors.Normal}"
#else
            "Outgoing client Request: {Details}"
#endif
            , requestDescriptor);

        var response = await base.SendAsync(request, cancellationToken);

        var responseDescriptor = FormatResponse(response);
        _recorder.TraceInformation(caller.ToCall(),
#if TESTINGONLY
            $"Incoming client response: {ConsoleConstants.Colors.Cyan}{{Details}}{ConsoleConstants.Colors.Normal}"
#else
            "Outgoing client Response: {Details}"
#endif
            , responseDescriptor);

        return response;
    }

    private async Task<string> FormatRequestAsync(HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        var headersValues = httpRequest.Headers
            .Concat(httpRequest.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .Where(h => RequestHeadersOfInterest.ContainsIgnoreCase(h.Key))
            .Select(h => $"{h.Key}={h.Value.JoinAsOredChoices()}")
            .JoinAsOredChoices();
        var headers = headersValues.HasValue()
            ? $" [{headersValues}]"
            : string.Empty;

        var body = string.Empty;
        if (_recorder.TraceLevel == RecorderTraceLevel.Debug)
        {
            if (httpRequest.Method.CanHaveBody()
                && httpRequest.Content.Exists())
            {
                var bodyValue = await GetRequestBodyAsync(httpRequest, cancellationToken);
                body = bodyValue.HasValue()
                    ? $" with body: {bodyValue}"
                    : string.Empty;
            }
        }

        return
            $"{httpRequest.Method} {httpRequest.RequestUri!.PathAndQuery} Sent, with{headers}{body}";
    }

    private static async Task<string?> GetRequestBodyAsync(HttpRequestMessage httpRequest,
        CancellationToken cancellationToken)
    {
        if (!httpRequest.Method.CanHaveBody())
        {
            return null;
        }

        if (httpRequest.Content.NotExists())
        {
            return "(empty)";
        }

        var body = await httpRequest.Content.ReadAsStringAsync(cancellationToken);
        if (body.Length == 0)
        {
            return "(empty)";
        }

        var sanitizedBody = (body.Length > MaxBodySize
                ? body[..MaxBodySize] + "..."
                : body)
            .Replace("\n", "\\n")
            .Replace("\r", "\\r"); //we want to see the newlines in the log, but not have newlines in the log
        return sanitizedBody;
    }

    private static string FormatResponse(HttpResponseMessage response)
    {
        var statusCodeDescription = $"{response.StatusCode} - {response.ReasonPhrase}";

        var headersValues = response.Headers
            .Concat(response.Content.Headers)
            .Select(h => $"{h.Key}={h.Value.JoinAsOredChoices()}")
            .JoinAsOredChoices();
        var headers = headersValues.HasValue()
            ? $" [{headersValues}]"
            : string.Empty;

        return
            $"{statusCodeDescription} Received, with{headers}";
    }
}