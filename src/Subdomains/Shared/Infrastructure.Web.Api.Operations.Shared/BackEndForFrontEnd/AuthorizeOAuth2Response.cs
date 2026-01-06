using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

public class AuthorizeOAuth2Response : IWebResponse
{
    public required AuthorizeRedirect Redirect { get; set; }
}

public class AuthorizeRedirect
{
    public required string RedirectUri { get; set; }

    public bool IsLogin { get; set; }

    public bool IsConsent { get; set; }

    public bool IsExternal { get; set; }
}