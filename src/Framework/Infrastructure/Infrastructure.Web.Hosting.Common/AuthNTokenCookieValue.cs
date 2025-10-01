namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Defines the value stored inside an authentication token cookie
/// </summary>
public class AuthNTokenCookieValue
{
    public DateTime? ExpiresOn { get; set; }

    public required string Token { get; set; } = string.Empty;
}