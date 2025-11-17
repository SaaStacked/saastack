using Application.Common;
using Application.Interfaces;
using Common;
using Domain.Interfaces;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a <see cref="ICallerContext" /> that does nothing
/// </summary>
public class NoOpCallerContext : ICallerContext
{
    public static NoOpCallerContext Instance { get; } = new();

    public Optional<ICallerContext.CallerAuthorization> Authorization =>
        Optional<ICallerContext.CallerAuthorization>.None;

    public string CallerId => CallerConstants.AnonymousUserId;

    public string CallId => Caller.GenerateCallId();

    public ICallerContext.CallerFeatures Features => new();

    public DatacenterLocation HostRegion => DatacenterLocations.Local;

    public bool IsAuthenticated => false;

    public bool IsServiceAccount => false;

    public ICallerContext.CallerRoles Roles => new();

    public Optional<string> TenantId => Optional<string>.None;
}