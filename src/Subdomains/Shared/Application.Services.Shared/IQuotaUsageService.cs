using Application.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service that can be used to update quotas on certain features
/// </summary>
public interface IQuotaUsageService
{
    /// <summary>
    ///     Checks to see if the <see cref="proposedTotal" /> for the specified <see cref="quotaId" />,
    ///     exceeds the allowed quota, and if so returns OK. Otherwise, returns a <see cref="ErrorCode.FeatureViolation" />.
    /// </summary>
    Task<Result<Error>> TryCheckQuotaUsageAsync(ICallerContext caller, string quotaId, long proposedTotal,
        CancellationToken cancellationToken);
}