using Application.Persistence.Common;
using Common;
using QueryAny;

namespace IdentityApplication.Persistence.ReadModels;

[EntityName("APIKeyAuth")]
public class APIKeyAuth : ReadModelEntity
{
    public Optional<string> Description { get; set; }

    public Optional<DateTime?> ExpiresOn { get; set; }

    public Optional<string> KeyToken { get; set; }

    public Optional<DateTime?> RevokedOn { get; set; }

    public Optional<string> UserId { get; set; }
}