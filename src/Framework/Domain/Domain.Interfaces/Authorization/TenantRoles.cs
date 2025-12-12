using Common.Extensions;

namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines the available organization member scoped roles
///     (i.e. access to tenanted resources by members of organizations)
/// </summary>
public static class TenantRoles
{
    public static readonly RoleLevel Member = new("tnt_mem");
    public static readonly RoleLevel Owner = new("tnt_own", Member);
    public static readonly RoleLevel BillingAdmin = new("tnt_bill_adm", Owner);
    public static readonly RoleLevel TestingOnly = new("tnt_testingonly");
    public static readonly Dictionary<string, RoleLevel> AllRoles = new()
    {
        // EXTEND: Add all new roles here
        { Member.Name, Member },
        { Owner.Name, Owner },
        { BillingAdmin.Name, BillingAdmin },
#if TESTINGONLY
        { TestingOnly.Name, TestingOnly },
#endif
    };

    private static readonly IReadOnlyList<RoleLevel> TenantAssignableRoles = new List<RoleLevel>
    {
        // EXTEND: Add new roles that can be assigned/unassigned to EndUsers
        Member,
        Owner,
        BillingAdmin,

#if TESTINGONLY
        TestingOnly
#endif
    };

    /// <summary>
    ///     Returns the <see cref="RoleLevel" /> for the specified <see cref="name" /> of the role
    /// </summary>
    public static RoleLevel? FindRoleByName(string name)
    {
#if NETSTANDARD2_0
        return AllRoles.TryGetValue(name, out var role)
            ? role
            : null;
#else
        return AllRoles.GetValueOrDefault(name);
#endif
    }

    /// <summary>
    ///     Whether the specified <see cref="role" /> is assignable
    /// </summary>
    public static bool IsTenantAssignableRole(string role)
    {
        return TenantAssignableRoles
            .Select(rol => rol.Name)
            .ContainsIgnoreCase(role);
    }
}