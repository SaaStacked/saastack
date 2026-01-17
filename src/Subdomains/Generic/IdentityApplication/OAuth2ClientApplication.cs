using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace IdentityApplication;

public class OAuth2ClientApplication : IOAuth2ClientApplication
{
    private readonly IIdentityServerProvider _identityServerProvider;

    public OAuth2ClientApplication(IIdentityServerProvider identityServerProvider)
    {
        _identityServerProvider = identityServerProvider;
    }

    public async Task<Result<OAuth2Client, Error>> ChangeClientLogoAsync(ICallerContext caller, string id,
        FileUpload upload, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OAuth2ClientService.ChangeClientLogoAsync(caller, id, upload,
            cancellationToken);
    }

    public async Task<Result<OAuth2ClientConsentResult, Error>> ConsentToClientAsync(ICallerContext caller,
        string clientId, string redirectUri, string scope, bool consented, CancellationToken cancellationToken)
    {
        var userId = caller.CallerId;
        return await _identityServerProvider.OAuth2ClientService.ConsentToClientAsync(caller, clientId, userId,
            redirectUri, scope,
            consented, cancellationToken);
    }

    public async Task<Result<OAuth2Client, Error>> CreateClientAsync(ICallerContext caller, string name,
        string? redirectUri, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OAuth2ClientService.CreateClientAsync(caller, name, redirectUri,
            cancellationToken);
    }

    public async Task<Result<Error>> DeleteClientAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OAuth2ClientService.DeleteClientAsync(caller, id, cancellationToken);
    }

    public async Task<Result<OAuth2Client, Error>> DeleteClientLogoAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OAuth2ClientService.DeleteClientLogoAsync(caller, id,
            cancellationToken);
    }

    public async Task<Result<OAuth2ClientWithSecrets, Error>> GetClientAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OAuth2ClientService.GetClientAsync(caller, id, cancellationToken);
    }

    public async Task<Result<OAuth2ClientConsent, Error>> GetConsentAsync(ICallerContext caller, string clientId,
        CancellationToken cancellationToken)
    {
        var userId = caller.CallerId;
        return await _identityServerProvider.OAuth2ClientService.GetConsentAsync(caller, clientId, userId,
            cancellationToken);
    }

    public async Task<Result<OAuth2ClientConsentStatus, Error>> HasUserConsentedClientAsync(ICallerContext caller,
        string clientId, string scope,
        CancellationToken cancellationToken)
    {
        var userId = caller.CallerId;
        return await _identityServerProvider.OAuth2ClientService.HasUserConsentedClientAsync(caller, clientId, userId,
            scope,
            cancellationToken);
    }

    public async Task<Result<OAuth2ClientWithSecret, Error>> RegenerateClientSecretAsync(ICallerContext caller,
        string id, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OAuth2ClientService.RegenerateClientSecretAsync(caller, id,
            cancellationToken);
    }

    public async Task<Result<Error>> RevokeConsentAsync(ICallerContext caller, string clientId,
        CancellationToken cancellationToken)
    {
        var userId = caller.CallerId;
        return await _identityServerProvider.OAuth2ClientService.RevokeConsentAsync(caller, clientId, userId,
            cancellationToken);
    }

    public async Task<Result<SearchResults<OAuth2Client>, Error>> SearchAllClientsAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OAuth2ClientService.SearchAllClientsAsync(caller, searchOptions,
            getOptions, cancellationToken);
    }

    public async Task<Result<OAuth2Client, Error>> UpdateClientAsync(ICallerContext caller, string id, string? name,
        string? redirectUri, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OAuth2ClientService.UpdateClientAsync(caller, id, name, redirectUri,
            cancellationToken);
    }
}