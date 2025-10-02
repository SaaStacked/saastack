namespace Common.Recording;

/// <summary>
///     Records measurements taken in the system
/// </summary>
public interface IMetricReporter
{
    void Measure(ICallContext? call, string eventName, Dictionary<string, object>? additional = null);
}