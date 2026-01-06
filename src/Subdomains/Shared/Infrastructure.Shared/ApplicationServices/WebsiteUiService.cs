using Application.Interfaces.Services;
using Application.Services.Shared;
using Common.Extensions;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a service for constructing resources based on a known Website UI Application
/// </summary>
public sealed class WebsiteUiService : IWebsiteUiService
{
    //EXTEND: these URLs must reflect routes used by the website in App.tsx
    public const string LoginPageRoute = "/identity/credentials/login";
    public const string OAuth2ConsentPageRoute = "/identity/oauth2/client/consent";
    public const string PasswordMfaOobConfirmationPageRoute = "/identity/credentials/2fa/mfaoob-confirm";
    public const string PasswordRegistrationConfirmationPageRoute = "/identity/credentials/register-confirm";
    public const string PasswordResetConfirmationPageRoute = "/identity/credentials/password-reset-complete";
    public const string RegistrationPageRoute = "/identity/credentials/register";
    private readonly string _websiteHostBaseUrl;

    public WebsiteUiService(IHostSettings hostSettings)
    {
        _websiteHostBaseUrl = hostSettings.GetWebsiteHostBaseUrl().WithoutTrailingSlash();
    }

    public string ConstructLoginPageUrl()
    {
        return $"{_websiteHostBaseUrl}/{LoginPageRoute.WithoutLeadingSlash()}";
    }

    public string ConstructOAuth2ConsentPageUrl(string clientId, string redirectUri, string scope, string? state)
    {
        var stateParam = state.HasValue()
            ? $"&state={state}"
            : string.Empty;
        return
            $"{_websiteHostBaseUrl}/{OAuth2ConsentPageRoute.WithoutLeadingSlash()}?client_id={clientId}&scope={scope}&redirect_uri={redirectUri}{stateParam}";
    }

    public string ConstructPasswordMfaOobConfirmationPageUrl(string code)
    {
        var escapedCode = Uri.EscapeDataString(code);
        return $"{_websiteHostBaseUrl}/{PasswordMfaOobConfirmationPageRoute.WithoutLeadingSlash()}?code={escapedCode}";
    }

    public string ConstructPasswordRegistrationConfirmationPageUrl(string token)
    {
        var escapedToken = Uri.EscapeDataString(token);
        return
            $"{_websiteHostBaseUrl}/{PasswordRegistrationConfirmationPageRoute.WithoutLeadingSlash()}?token={escapedToken}";
    }

    public string ConstructPasswordResetConfirmationPageUrl(string token)
    {
        var escapedToken = Uri.EscapeDataString(token);
        return $"{_websiteHostBaseUrl}/{PasswordResetConfirmationPageRoute.WithoutLeadingSlash()}?token={escapedToken}";
    }

    public string CreateRegistrationPageUrl(string token)
    {
        var escapedToken = Uri.EscapeDataString(token);
        return $"{_websiteHostBaseUrl}/{RegistrationPageRoute.WithoutLeadingSlash()}?token={escapedToken}";
    }
}