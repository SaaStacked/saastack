using Application.Interfaces;
using Infrastructure.Interfaces;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a <see cref="ICallerContextFactory" /> that returns a <see cref="NoOpCallerContext" />
/// </summary>
public class NoOpCallerContextFactory : ICallerContextFactory
{
    public static NoOpCallerContextFactory Instance { get; } = new();

    public ICallerContext Create()
    {
        return NoOpCallerContext.Instance;
    }
}