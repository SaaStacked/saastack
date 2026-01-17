using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Services.Shared;
using Domain.Shared;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Provides a native OAuth2 client management service for managing and persisting OAuth2 clients
///     OAuth2 Specification: <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-2" />
/// </summary>
public class NativeIdentityServerOAuth2ClientService : IIdentityServerOAuth2ClientService
{
    private readonly IOAuth2ClientConsentRepository _clientConsentRepository;
    private readonly IOAuth2ClientRepository _clientRepository;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IImagesService _imagesService;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IRecorder _recorder;
    private readonly ITokensService _tokensService;

    public NativeIdentityServerOAuth2ClientService(IRecorder recorder, IIdentifierFactory identifierFactory,
        ITokensService tokensService, IPasswordHasherService passwordHasherService,
        IImagesService imagesService, IOAuth2ClientRepository clientRepository,
        IOAuth2ClientConsentRepository clientConsentRepository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _tokensService = tokensService;
        _passwordHasherService = passwordHasherService;
        _clientRepository = clientRepository;
        _clientConsentRepository = clientConsentRepository;
        _imagesService = imagesService;
    }

    public async Task<Result<OAuth2Client, Error>> ChangeClientLogoAsync(ICallerContext caller, string id,
        FileUpload upload, CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        var logoed = await client.ChangeLogoAsync(async name =>
        {
            var created = await _imagesService.CreateImageAsync(caller, upload, name.Text, cancellationToken);
            if (created.IsFailure)
            {
                return created.Error;
            }

            return Avatar.Create(created.Value.Id.ToId(), created.Value.Url);
        }, async logoId =>
        {
            var removed = await _imagesService.DeleteImageAsync(caller, logoId, cancellationToken);
            return removed.IsFailure
                ? removed.Error
                : Result.Ok;
        });
        if (logoed.IsFailure)
        {
            return logoed.Error;
        }

        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        client = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Changed logo for OAuth2 client: {Id}", client.Id);

        return client.ToClient();
    }

    public async Task<Result<OAuth2ClientConsentResult, Error>> ConsentToClientAsync(ICallerContext caller,
        string clientId, string userId, string redirectUri, string scope, bool isConsented,
        CancellationToken cancellationToken)
    {
        var retrievedClient = await _clientRepository.LoadAsync(clientId.ToId(), cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return retrievedClient.Error;
        }

        var retrieved = await _clientConsentRepository.FindByUserId(clientId.ToId(), userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var consentedScopes = OAuth2Scopes.Create(scope);
        if (consentedScopes.IsFailure)
        {
            return consentedScopes.Error;
        }

        OAuth2ClientConsentRoot consent;
        if (retrieved.Value.HasValue)
        {
            consent = retrieved.Value.Value;
        }
        else
        {
            var created = OAuth2ClientConsentRoot.Create(_recorder, _identifierFactory, clientId.ToId(), userId.ToId());
            if (created.IsFailure)
            {
                return created.Error;
            }

            consent = created.Value;
        }

        if (isConsented)
        {
            var consented = consent.ChangeConsent(userId.ToId(), isConsented, consentedScopes.Value);
            if (consented.IsFailure)
            {
                return consented.Error;
            }
        }
        else
        {
            var revoked = consent.Revoke(userId.ToId());
            if (revoked.IsFailure)
            {
                return revoked.Error;
            }
        }

        var saved = await _clientConsentRepository.SaveAsync(consent, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        consent = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), consent.IsConsented
            ? "Client {Id} was consented by user {UserId}"
            : "Client {Id} was un-consented by user {UserId}", consent.ClientId, consent.UserId);

        return new OAuth2ClientConsentResult
        {
            Consent = consent.IsConsented
                ? consent.ToConsent()
                : null,
            DenyError = consent.IsConsented
                ? null
                : Error.Validation(Resources.ClientsApi_ConsentClientForCaller_ClientRevoked,
                    OAuth2Constants.ErrorCodes.AccessDenied)
        };
    }

    public async Task<Result<OAuth2Client, Error>> CreateClientAsync(ICallerContext caller, string name,
        string? redirectUri, CancellationToken cancellationToken)
    {
        var clientName = Name.Create(name);
        if (clientName.IsFailure)
        {
            return clientName.Error;
        }

        var created = OAuth2ClientRoot.Create(_recorder, _identifierFactory, _tokensService, _passwordHasherService,
            clientName.Value);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var client = created.Value;
        if (redirectUri.HasValue())
        {
            var updated = client.ChangeRedirectUri(redirectUri);
            if (updated.IsFailure)
            {
                return updated.Error;
            }
        }

        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        client = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Client {Id} created", client.Id);

        return client.ToClient();
    }

    public async Task<Result<Error>> DeleteClientAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        var deleted = client.Delete(caller.ToCallerId());
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Client {Id} deleted", client.Id);

        return Result.Ok;
    }

    public async Task<Result<OAuth2Client, Error>> DeleteClientLogoAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        var deleted = await client.RemoveLogoAsync(async logoId =>
        {
            var removed = await _imagesService.DeleteImageAsync(caller, logoId, cancellationToken);
            return removed.IsFailure
                ? removed.Error
                : Result.Ok;
        });
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        client = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "OAuth2 client {Id} logo was deleted", client.Id);

        return client.ToClient();
    }

    public async Task<Result<Optional<OAuth2Client>, Error>> FindClientByIdAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.FindById(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        if (!client.HasValue)
        {
            return Optional<OAuth2Client>.None;
        }

        return client.Value.ToClient().ToOptional();
    }

    public async Task<Result<OAuth2ClientWithSecrets, Error>> GetClientAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Client {Id} was fetched", client.Id);

        return client.ToClientWithSecrets();
    }

    public async Task<Result<OAuth2ClientConsent, Error>> GetConsentAsync(ICallerContext caller, string clientId,
        string userId, CancellationToken cancellationToken)
    {
        var retrievedClient = await _clientRepository.LoadAsync(clientId.ToId(), cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return retrievedClient.Error;
        }

        var retrieved = await _clientConsentRepository.FindByUserId(clientId.ToId(), userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return new OAuth2ClientConsent
            {
                Id = string.Empty,
                ClientId = clientId,
                IsConsented = false,
                Scopes = [],
                UserId = userId
            };
        }

        var consent = retrieved.Value.Value;
        _recorder.TraceInformation(caller.ToCall(), "Consent for client {ClientId} and user {UserId} was retrieved",
            consent.ClientId, consent.UserId);

        return consent.ToConsent();
    }

    public async Task<Result<OAuth2ClientConsentStatus, Error>> HasUserConsentedClientAsync(ICallerContext caller,
        string clientId,
        string userId, string scope, CancellationToken cancellationToken)
    {
        var retrievedClient = await _clientRepository.LoadAsync(clientId.ToId(), cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return retrievedClient.Error;
        }

        var client = retrievedClient.Value;
        var retrieved = await _clientConsentRepository.FindByUserId(clientId.ToId(), userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var scopes = OAuth2Scopes.Create(scope);
        if (scopes.IsFailure)
        {
            return scopes.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return new OAuth2ClientConsentStatus
            {
                Client = client.ToClient(),
                IsConsented = false,
                Scopes = scopes.Value.Items,
                UserId = userId
            };
        }

        var consent = retrieved.Value.Value;
        var consented = consent.HasConsentedTo(scopes.Value);

        return new OAuth2ClientConsentStatus
        {
            Client = client.ToClient(),
            IsConsented = consented,
            Scopes = scopes.Value.Items,
            UserId = userId
        };
    }

    public async Task<Result<OAuth2ClientWithSecret, Error>> RegenerateClientSecretAsync(ICallerContext caller,
        string id, CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        var generated = client.GenerateSecret(Optional<TimeSpan>.None);
        if (generated.IsFailure)
        {
            return generated.Error;
        }

        var secret = generated.Value;
        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        client = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Client {Id} generated a new secret", client.Id);

        return client.ToClientWithSecret(secret);
    }

    public async Task<Result<Error>> RevokeConsentAsync(ICallerContext caller, string clientId, string userId,
        CancellationToken cancellationToken)
    {
        var retrievedClient = await _clientRepository.LoadAsync(clientId.ToId(), cancellationToken);
        if (retrievedClient.IsFailure)
        {
            return retrievedClient.Error;
        }

        var retrieved = await _clientConsentRepository.FindByUserId(clientId.ToId(), userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        OAuth2ClientConsentRoot consent;
        if (retrieved.Value.HasValue)
        {
            consent = retrieved.Value.Value;
        }
        else
        {
            var created = OAuth2ClientConsentRoot.Create(_recorder, _identifierFactory, clientId.ToId(), userId.ToId());
            if (created.IsFailure)
            {
                return created.Error;
            }

            consent = created.Value;
        }

        var revoked = consent.Revoke(userId.ToId());
        if (revoked.IsFailure)
        {
            return revoked.Error;
        }

        var saved = await _clientConsentRepository.SaveAsync(consent, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        consent = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Consent for client {ClientId} and user {UserId} was revoked",
            consent.ClientId, consent.UserId);

        return Result.Ok;
    }

    public async Task<Result<SearchResults<OAuth2Client>, Error>> SearchAllClientsAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken)
    {
        var searched = await _clientRepository.SearchAllAsync(searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var clients = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All clients were fetched");

        return clients.ToSearchResults(searchOptions, client => client.ToClient());
    }

    public async Task<Result<OAuth2Client, Error>> UpdateClientAsync(ICallerContext caller, string id, string? name,
        string? redirectUri, CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        if (name.HasValue())
        {
            var clientName = Name.Create(name);
            if (clientName.IsFailure)
            {
                return clientName.Error;
            }

            var updated = client.ChangeName(clientName.Value);
            if (updated.IsFailure)
            {
                return updated.Error;
            }
        }

        if (redirectUri.HasValue())
        {
            var updated = client.ChangeRedirectUri(redirectUri);
            if (updated.IsFailure)
            {
                return updated.Error;
            }
        }

        var saved = await _clientRepository.SaveAsync(client, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        client = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Client {Id} updated", client.Id);

        return client.ToClient();
    }

    public async Task<Result<OAuth2Client, Error>> VerifyClientAsync(ICallerContext caller, string id,
        string clientSecret, CancellationToken cancellationToken)
    {
        var retrieved = await _clientRepository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var client = retrieved.Value;
        var verified = client.VerifySecret(clientSecret);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Client {Id} verified", client.Id);

        return client.ToClient();
    }
}

public static class NativeIdentityServerOAuth2ClientServiceConversionExtensions
{
    public static OAuth2Client ToClient(this OAuth2ClientRoot client)
    {
        return new OAuth2Client
        {
            Id = client.Id,
            LogoUrl = client.Logo.ToNullable(c => c.Url),
            Name = client.Name.ValueOrDefault!,
            RedirectUri = client.RedirectUri.ValueOrDefault!
        };
    }

    public static OAuth2Client ToClient(this Persistence.ReadModels.OAuth2Client client)
    {
        return new OAuth2Client
        {
            Id = client.Id,
            LogoUrl = client.LogoUrl,
            Name = client.Name,
            RedirectUri = client.RedirectUri
        };
    }

    public static OAuth2ClientWithSecret ToClientWithSecret(this OAuth2ClientRoot client, GeneratedClientSecret secret)
    {
        return new OAuth2ClientWithSecret
        {
            Id = client.Id,
            LogoUrl = client.Logo.ToNullable(c => c.Url),
            Name = client.Name.ValueOrDefault!,
            RedirectUri = client.RedirectUri,
            Secret = secret.PlainSecret,
            ExpiresOnUtc = secret.ExpiresOn.ToNullable()
        };
    }

    public static OAuth2ClientWithSecrets ToClientWithSecrets(this OAuth2ClientRoot client)
    {
        return new OAuth2ClientWithSecrets
        {
            Id = client.Id,
            LogoUrl = client.Logo.ToNullable(c => c.Url),
            Name = client.Name.ValueOrDefault!,
            RedirectUri = client.RedirectUri,
            Secrets = client.Secrets.Select(sec => new OAuthClientSecret
            {
                ExpiresOnUtc = sec.ExpiresOn.ToNullable(),
                Reference = $"{sec.FirstFour}********************"
            }).ToList()
        };
    }

    public static OAuth2ClientConsent ToConsent(this OAuth2ClientConsentRoot consent)
    {
        return new OAuth2ClientConsent
        {
            Id = consent.Id,
            ClientId = consent.ClientId,
            IsConsented = consent.IsConsented,
            Scopes = consent.Scopes.Items,
            UserId = consent.UserId
        };
    }
}