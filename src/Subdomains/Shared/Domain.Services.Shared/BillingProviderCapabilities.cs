using Domain.Shared.Subscriptions;

namespace Domain.Services.Shared;

/// <summary>
///     Defines the capabilities of this provider
/// </summary>
public class BillingProviderCapabilities
{
    /// <summary>
    ///     When not self-managing features, return a map of features for each tier
    /// </summary>
    public IReadOnlyDictionary<BillingSubscriptionTier, IReadOnlyList<ProviderPlanFeatureSection>>? ManagedFeatures
    {
        get;
        init;
    }

    /// <summary>
    ///     When not self-managing quotas, return a map of quotas for each tier
    /// </summary>
    public ProviderQuotas? ManagedQuotas { get; init; }

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
    ///     Whether the provider supports quotas, and manages the state of its own quotas
    /// </summary>
    public ManagementOptions QuotaManagement { get; init; } = ManagementOptions.SelfManaged;

    /// <summary>
    ///     Whether the provider supports trials, and manages the state of its own trials
    /// </summary>
    public ManagementOptions TrialManagement { get; init; } = ManagementOptions.SelfManaged;
}

public enum ManagementOptions
{
    None = 0,
    SelfManaged = 1,
    RequiresManaged = 2
}