using Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Hosting.Common.Extensions;

public static class RecordingExtensions
{
    /// <summary>
    ///     Returns the level of tracing allowed by <see cref="ILogger" />
    /// </summary>
    public static RecorderTraceLevel GetTraceLevel(this ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Trace)
            || logger.IsEnabled(LogLevel.Debug))
        {
            return RecorderTraceLevel.Debug;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            return RecorderTraceLevel.Information;
        }

        return logger.IsEnabled(LogLevel.Warning)
            ? RecorderTraceLevel.Warning
            : RecorderTraceLevel.Error;
    }

    /// <summary>
    ///     Converts the <see cref="RecorderTraceLevel" /> to a <see cref="LogLevel" />
    /// </summary>
    public static LogLevel ToLoggerLevel(this RecorderTraceLevel level)
    {
        return level switch
        {
            RecorderTraceLevel.Debug => LogLevel.Debug,
            RecorderTraceLevel.Information => LogLevel.Information,
            RecorderTraceLevel.Warning => LogLevel.Warning,
            RecorderTraceLevel.Error => LogLevel.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };
    }
}