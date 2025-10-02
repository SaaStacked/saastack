using Common;

namespace Application.Interfaces;

/// <summary>
///     The context of the currently identified caller
/// </summary>
public partial interface ICallerContext
{
    /// <summary>
    ///     Defines the scheme of the authorization
    /// </summary>
    public enum AuthorizationMethod
    {
        Token = 0,
        APIKey = 1,
        HMAC = 2,
        PrivateInterHost = 3,
        AuthNCookie = 4
    }

    /// <summary>
    ///     The authorization token of the call. Passed to downstream clients
    /// </summary>
    Optional<CallerAuthorization> Authorization { get; }

    /// <summary>
    ///     The ID of the identified caller
    /// </summary>
    string CallerId { get; }

    /// <summary>
    ///     The ID of the (correlated) call
    /// </summary>
    string CallId { get; }

    /// <summary>
    ///     The authorization features belonging to the caller
    /// </summary>
    CallerFeatures Features { get; }

    /// <summary>
    ///     The region that this call is hosted within
    /// </summary>
    DatacenterLocation HostRegion { get; }

    /// <summary>
    ///     Whether the called is authenticated or not
    /// </summary>
    public bool IsAuthenticated { get; }

    /// <summary>
    ///     Whether the called is an internal service account
    /// </summary>
    public bool IsServiceAccount { get; }

    /// <summary>
    ///     The authorization roles belonging to the caller
    /// </summary>
    CallerRoles Roles { get; }

    /// <summary>
    ///     The ID of the tenant of the caller
    /// </summary>
    Optional<string> TenantId { get; }

    /// <summary>
    ///     Defines the authorization details of the caller
    /// </summary>
    public class CallerAuthorization
    {
        public CallerAuthorization(AuthorizationMethod method, Optional<string> value)
        {
            Method = method;
            Value = value;
        }

        public AuthorizationMethod Method { get; }

        public Optional<string> Value { get; }
    }
}