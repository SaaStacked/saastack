using Common.Recording;
using JetBrains.Annotations;

namespace Common;

/// <summary>
///     Defines a recorder that keeps note of various activities about the system for future examination
/// </summary>
public interface IRecorder
{
    RecorderTraceLevel TraceLevel { get; }

    void Audit(ICallContext? call, string auditCode, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void AuditAgainst(ICallContext? call, string againstId, string auditCode,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void Crash(ICallContext? call, CrashLevel level, Exception exception);

    void Crash(ICallContext? call, CrashLevel level, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void Measure(ICallContext? call, string eventName, Dictionary<string, object>? additional = null);

    void Trace(ICallContext? call, RecorderTraceLevel level, Exception? exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void TraceDebug(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void TraceError(ICallContext? call, Exception exception, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void TraceError(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void TraceInformation(ICallContext? call, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void TraceInformation(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void TraceWarning(ICallContext? call, Exception exception, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void TraceWarning(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);

    void TrackUsage(ICallContext? call, string eventName, Dictionary<string, object>? additional = null);

    void TrackUsageFor(ICallContext? call, string forId, string eventName,
        Dictionary<string, object>? additional = null);
}

/// <summary>
///     Trace level for all recording
/// </summary>
public enum RecorderTraceLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3
}