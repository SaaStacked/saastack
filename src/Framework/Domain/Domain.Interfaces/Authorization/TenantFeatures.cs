using Common.Extensions;

namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines the available tenant member scoped features
///     (i.e. access to tenanted resources by members of organizations)
/// </summary>
public static class TenantFeatures
{
    public static readonly FeatureLevel
        Basic = new("tenant_basic_features"); // Free/Basic limited features, everyone can use
    public static readonly FeatureLevel
        PaidTrial = new("tenant_paidtrial_features", Basic); // a.k.a Standard plan features
    public static readonly FeatureLevel
        Paid2 = new("tenant_paid2_features", PaidTrial); // a.k.a Professional plan features
    public static readonly FeatureLevel Paid3 = new("tenant_paid3_features", Paid2); // a.k.a Enterprise plan features
    public static readonly FeatureLevel TestingOnly = new("tenant_testingonly_platform");
    public static readonly Dictionary<string, FeatureLevel> AllFeatures = new()
    {
        { Basic.Name, Basic },
        { PaidTrial.Name, PaidTrial },
        { Paid2.Name, Paid2 },
        { Paid3.Name, Paid3 },
#if TESTINGONLY
        { TestingOnly.Name, TestingOnly }
#endif
    };

    // EXTEND: Add other features that Members can be assigned to control access to tenanted resources (e.g. tenanted APIs)

    public static readonly IReadOnlyList<FeatureLevel> TenantAssignableFeatures = new List<FeatureLevel>
    {
        // EXTEND: Add new features that can be assigned/unassigned to EndUsers
        Basic,
        PaidTrial,
        Paid2,
        Paid3,
#if TESTINGONLY
        TestingOnly
#endif
    };

    /// <summary>
    ///     Returns the <see cref="FeatureLevel" /> for the specified <see cref="name" /> of the feature
    /// </summary>
    public static FeatureLevel? FindFeatureByName(string name)
    {
#if NETSTANDARD2_0
        return AllFeatures.TryGetValue(name, out var feature)
            ? feature
            : null;
#else
        return AllFeatures.GetValueOrDefault(name);
#endif
    }

    /// <summary>
    ///     Whether the <see cref="feature" /> is assignable
    /// </summary>
    public static bool IsTenantAssignableFeature(string feature)
    {
        return TenantAssignableFeatures
            .Select(feat => feat.Name)
            .ContainsIgnoreCase(feature);
    }
}