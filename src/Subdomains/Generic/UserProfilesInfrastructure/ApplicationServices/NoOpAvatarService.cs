using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace UserProfilesInfrastructure.ApplicationServices;

/// <summary>
///     Provides a <see cref="IAvatarService" /> that does nothing
/// </summary>
public class NoOpAvatarService : IAvatarService
{
    public async Task<Result<Optional<FileUpload>, Error>> FindAvatarAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return Optional<FileUpload>.None;
    }
}