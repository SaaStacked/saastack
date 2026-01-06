using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetOAuth2ClientConsentStatusResponse : IWebResponse
{
    public required OAuth2ClientConsentStatus Status { get; set; }
}