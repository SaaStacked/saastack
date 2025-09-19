#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetPersonCredentialRegistrationConfirmationResponse : IWebResponse
{
    public required string Token { get; set; }
}
#endif