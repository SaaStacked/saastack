using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class InitiatePasswordResetResponse : IWebResponse
{
    public required string ResendToken { get; set; }
}