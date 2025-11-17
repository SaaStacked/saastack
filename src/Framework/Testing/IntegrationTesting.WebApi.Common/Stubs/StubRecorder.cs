using Common;
using Common.Recording;

namespace IntegrationTesting.WebApi.Common.Stubs;

/// <summary>
///     Provides a stub for testing <see cref="IRecorder" />
/// </summary>
public sealed class StubRecorder : IRecorder
{
    public string? LastAuditAuditCode { get; private set; }

    public object[]? LastCrashArguments { get; private set; }

    public Exception? LastCrashException { get; private set; }

    public CrashLevel? LastCrashLevel { get; private set; }

    public string? LastCrashMessageTemplate { get; private set; }

    public Dictionary<string, object>? LastMeasureAdditional { get; private set; }

    public string? LastMeasureEventName { get; private set; }

    public object[]? LastTraceArguments { get; private set; }

    public RecorderTraceLevel? LastTraceLevel { get; private set; }

    public List<TraceMessage> LastTraceMessages { get; } = new();

    public string? LastTraceMessageTemplate { get; private set; }

    public Dictionary<string, object>? LastUsageAdditional { get; private set; }

    public string? LastUsageEventName { get; private set; }

    public void Audit(ICallContext? call, string auditCode, string messageTemplate, params object[] templateArgs)
    {
        LastAuditAuditCode = auditCode;
    }

    public void AuditAgainst(ICallContext? call, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
        LastAuditAuditCode = auditCode;
    }

    public void Crash(ICallContext? call, CrashLevel level, Exception exception)
    {
        LastCrashLevel = level;
        LastCrashException = exception;
        LastCrashMessageTemplate = null;
        LastCrashArguments = null;
    }

    public void Crash(ICallContext? call, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        LastCrashLevel = level;
        LastCrashException = exception;
        LastCrashMessageTemplate = messageTemplate;
        LastCrashArguments = templateArgs;
    }

    public void Measure(ICallContext? call, string eventName, Dictionary<string, object>? additional = null)
    {
        LastMeasureEventName = eventName;
        LastMeasureAdditional = additional;
    }

    public void Trace(ICallContext? call, RecorderTraceLevel level, Exception? exception, string messageTemplate,
        params object[] templateArgs)
    {
        LastTraceLevel = level;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
        LastTraceMessages.Add(new TraceMessage(level, messageTemplate, templateArgs));
    }

    public void TraceDebug(ICallContext? call, string messageTemplate, params object[] templateArgs)
    {
        LastTraceLevel = RecorderTraceLevel.Debug;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
        LastTraceMessages.Add(new TraceMessage(RecorderTraceLevel.Debug, messageTemplate, templateArgs));
    }

    public void TraceError(ICallContext? call, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        LastTraceLevel = RecorderTraceLevel.Error;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
        LastTraceMessages.Add(new TraceMessage(RecorderTraceLevel.Error, messageTemplate, templateArgs));
    }

    public void TraceError(ICallContext? call, string messageTemplate, params object[] templateArgs)
    {
        LastTraceLevel = RecorderTraceLevel.Error;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
        LastTraceMessages.Add(new TraceMessage(RecorderTraceLevel.Error, messageTemplate, templateArgs));
    }

    public void TraceInformation(ICallContext? call, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        LastTraceLevel = RecorderTraceLevel.Information;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
        LastTraceMessages.Add(new TraceMessage(RecorderTraceLevel.Information, messageTemplate, templateArgs));
    }

    public void TraceInformation(ICallContext? call, string messageTemplate, params object[] templateArgs)
    {
        LastTraceLevel = RecorderTraceLevel.Information;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
        LastTraceMessages.Add(new TraceMessage(RecorderTraceLevel.Information, messageTemplate, templateArgs));
    }

    public RecorderTraceLevel TraceLevel { get; } = RecorderTraceLevel.Information;

    public void TraceWarning(ICallContext? call, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        LastTraceLevel = RecorderTraceLevel.Warning;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
        LastTraceMessages.Add(new TraceMessage(RecorderTraceLevel.Warning, messageTemplate, templateArgs));
    }

    public void TraceWarning(ICallContext? call, string messageTemplate, params object[] templateArgs)
    {
        LastTraceLevel = RecorderTraceLevel.Warning;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
        LastTraceMessages.Add(new TraceMessage(RecorderTraceLevel.Warning, messageTemplate, templateArgs));
    }

    public void TrackUsage(ICallContext? call, string eventName, Dictionary<string, object>? additional = null)
    {
        LastUsageEventName = eventName;
        LastUsageAdditional = additional;
    }

    public void TrackUsageFor(ICallContext? call, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
        LastUsageEventName = eventName;
        LastUsageAdditional = additional;
    }

    public void Reset()
    {
        LastTraceLevel = null;
        LastTraceMessageTemplate = null;
        LastTraceArguments = null;
        LastTraceMessages.Clear();
        LastCrashLevel = null;
        LastCrashException = null;
        LastCrashMessageTemplate = null;
        LastCrashArguments = null;
        LastMeasureEventName = null;
        LastMeasureAdditional = null;
        LastUsageEventName = null;
        LastUsageAdditional = null;
        LastAuditAuditCode = null;
    }
}

/// <summary>
///     A trace
/// </summary>
public record TraceMessage(RecorderTraceLevel Level, string Message, object[]? Arguments = null);