using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

public class ConsentOAuth2ClientResponse : IWebResponse
{
    public required ConsentRedirect Redirect { get; set; }
}

public class ConsentRedirect
{
    public string? RedirectUri { get; set; }

    public bool IsConsented { get; set; }
}