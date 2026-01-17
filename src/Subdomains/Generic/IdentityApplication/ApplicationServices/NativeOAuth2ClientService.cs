using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Services.Shared;
using IdentityApplication.Persistence;
using IdentityDomain.DomainServices;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a native service that manages OAuth2 clients
/// </summary>
public class NativeOAuth2ClientService : IOAuth2ClientService
{
    private readonly IIdentityServerOAuth2ClientService _service;

    public NativeOAuth2ClientService(IRecorder recorder, IIdentifierFactory identifierFactory,
        ITokensService tokensService, IPasswordHasherService passwordHasherService,
        IImagesService imagesService, IOAuth2ClientRepository oAuthClientRepository,
        IOAuth2ClientConsentRepository oAuthClientConsentRepository)
    {
        _service =
            new NativeIdentityServerOAuth2ClientService(recorder, identifierFactory, tokensService,
                passwordHasherService, imagesService, oAuthClientRepository, oAuthClientConsentRepository);
    }

    public async Task<Result<Optional<OAuth2Client>, Error>> FindClientByIdAsync(ICallerContext caller, string clientId,
        CancellationToken cancellationToken)
    {
        return await _service.FindClientByIdAsync(caller, clientId, cancellationToken);
    }

    public async Task<Result<bool, Error>> HasUserConsentedClientAsync(ICallerContext caller, string clientId,
        string userId, string scope, CancellationToken cancellationToken)
    {
        var consented = await _service.HasUserConsentedClientAsync(caller, clientId, userId, scope, cancellationToken);
        return consented.Match<Result<bool, Error>>(consent => consent.Value.IsConsented, error => error);
    }

    public async Task<Result<OAuth2Client, Error>> VerifyClientAsync(ICallerContext caller, string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        return await _service.VerifyClientAsync(caller, clientId, clientSecret, cancellationToken);
    }
}