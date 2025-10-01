using Application.Persistence.Common;
using Common;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("AuthToken")]
public class AuthToken : SnapshottedReadModelEntity
{
    // ReSharper disable once UnusedMember.Global
    public Optional<string> AccessToken { get; set; }

    // ReSharper disable once UnusedMember.Global
    public Optional<DateTime> AccessTokenExpiresOn { get; set; }

    // ReSharper disable once UnusedMember.Global
    public Optional<string> IdToken { get; set; }

    // ReSharper disable once UnusedMember.Global
    public Optional<DateTime> IdTokenExpiresOn { get; set; }

    // ReSharper disable once UnusedMember.Global
    public Optional<string> RefreshToken { get; set; }

    public Optional<string> RefreshTokenDigest { get; set; }

    // ReSharper disable once UnusedMember.Global
    public Optional<DateTime> RefreshTokenExpiresOn { get; set; }

    public Optional<string> UserId { get; set; }
}