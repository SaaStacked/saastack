using Domain.Shared.Subscriptions;

namespace Domain.Services.Shared;

/// <summary>
///     Defines the capabilities of this provider
/// </summary>
public class BillingProviderCapabilities
{
    /// <summary>
    ///     When not self-managing trials, return the duration of the managed trial
    /// </summary>
    public int ManagedTrialDurationDays { get; init; } = 7;

    /// <summary>
    ///     When not self-managing trials, return a schedule of events to send during the managed trial
    /// </summary>
    public TrialEventSchedule? ManagedTrialSchedule { get; init; }

    /// <summary>
    ///     Returns a list of usage events that are metered
    /// </summary>
    public IReadOnlyList<string> MeteredEvents { get; set; } = [];

    /// <summary>
    ///     Whether the provider supports trials, and manages the state of its own trials
    /// </summary>
    public TrialManagementOptions TrialManagement { get; init; } = TrialManagementOptions.SelfManaged;
}

public enum TrialManagementOptions
{
    None = 0,
    SelfManaged = 1,
    RequiresManaged = 2
}