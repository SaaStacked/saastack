using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.OAuth2.Clients;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;
using IdentityDomain.DomainServices;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class OAuth2ClientRoot : AggregateRootBase
{
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly ITokensService _tokensService;

    public static Result<OAuth2ClientRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        ITokensService tokensService, IPasswordHasherService passwordHasherService, Name name)
    {
        var root = new OAuth2ClientRoot(recorder, idFactory, tokensService, passwordHasherService);
        root.RaiseCreateEvent(IdentityDomain.Events.OAuth2.Clients.Created(root.Id, name));
        return root;
    }

    private OAuth2ClientRoot(IRecorder recorder, IIdentifierFactory idFactory, ITokensService tokensService,
        IPasswordHasherService passwordHasherService) : base(recorder, idFactory)
    {
        _tokensService = tokensService;
        _passwordHasherService = passwordHasherService;
    }

    private OAuth2ClientRoot(IRecorder recorder, IIdentifierFactory idFactory, ITokensService tokensService,
        IPasswordHasherService passwordHasherService, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
        _tokensService = tokensService;
        _passwordHasherService = passwordHasherService;
    }

    public Optional<Avatar> Logo { get; private set; }

    public Optional<Name> Name { get; private set; }

    public Optional<string> RedirectUri { get; private set; }

    public OAuth2ClientSecrets Secrets { get; } = [];

    [UsedImplicitly]
    public static AggregateRootFactory<OAuth2ClientRoot> Rehydrate()
    {
        return (identifier, container, _) => new OAuth2ClientRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(),
            container.GetRequiredService<ITokensService>(),
            container.GetRequiredService<IPasswordHasherService>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        var secrets = Secrets.EnsureInvariants();
        if (secrets.IsFailure)
        {
            return secrets.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                var name = Domain.Shared.Name.Create(created.Name);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                Name = name.Value;
                return Result.Ok;
            }

            case NameChanged changed:
            {
                var name = Domain.Shared.Name.Create(changed.Name);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                Name = name.Value;
                Recorder.TraceDebug(null, "OAuthClient {Id} has changed its name", Id);
                return Result.Ok;
            }

            case RedirectUriChanged changed:
            {
                RedirectUri = changed.RedirectUri;
                Recorder.TraceDebug(null, "OAuthClient {Id} has changed its redirect URI", Id);
                return Result.Ok;
            }

            case SecretAdded added:
            {
                var secret = OAuth2ClientSecret.Create(added.SecretHash, added.FirstFour, added.ExpiresOn.ToOptional());
                if (secret.IsFailure)
                {
                    return secret.Error;
                }

                Secrets.Add(secret.Value);
                Recorder.TraceDebug(null, "OAuthClient {Id} has added a secret", Id);
                return Result.Ok;
            }

            case LogoAdded added:
            {
                var logo = Avatar.Create(added.LogoId.ToId(), added.LogoUrl);
                if (logo.IsFailure)
                {
                    return logo.Error;
                }

                Logo = logo.Value;
                Recorder.TraceDebug(null, "OAuthClient {Id} added logo {Image}", Id, logo.Value.ImageId);
                return Result.Ok;
            }

            case LogoRemoved _:
            {
                Logo = Optional<Avatar>.None;
                Recorder.TraceDebug(null, "OAuthClient {Id} removed logo", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public async Task<Result<Error>> ChangeLogoAsync(CreateAvatarAction onCreateNew, RemoveAvatarAction onRemoveOld)
    {
        var existingLogoId = Logo.HasValue
            ? Logo.Value.ImageId.ToOptional()
            : Optional<Identifier>.None;
        var created = await onCreateNew(Domain.Shared.Name.Create(Name.Value.Text).Value);
        if (created.IsFailure)
        {
            return created.Error;
        }

        if (existingLogoId.HasValue)
        {
            var removed = await onRemoveOld(existingLogoId.Value);
            if (removed.IsFailure)
            {
                return removed.Error;
            }
        }

        return RaiseChangeEvent(IdentityDomain.Events.OAuth2.Clients.LogoAdded(Id, created.Value));
    }

    public Result<Error> ChangeName(Name name)
    {
        var nothingHasChanged = name == Name;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(IdentityDomain.Events.OAuth2.Clients.NameChanged(Id, name));
    }

    public Result<Error> ChangeRedirectUri(string redirectUri)
    {
        var nothingHasChanged = redirectUri == RedirectUri;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(IdentityDomain.Events.OAuth2.Clients.RedirectUriChanged(Id, redirectUri));
    }

    public Result<Error> Delete(Identifier deletedById)
    {
        return RaisePermanentDeleteEvent(IdentityDomain.Events.OAuth2.Clients.Deleted(Id, deletedById));
    }

    public Result<GeneratedClientSecret, Error> GenerateSecret(Optional<TimeSpan> duration)
    {
        var expiresOn = Optional<DateTime>.None;
        if (duration.HasValue)
        {
            expiresOn = DateTime.UtcNow
                .ToNearestMinute()
                .Add(duration.Value);
        }

        var secret = _tokensService.CreateOAuth2ClientSecret();
        var secret2 = OAuth2ClientSecret.Create(secret, expiresOn, _passwordHasherService);
        if (secret2.IsFailure)
        {
            return secret2.Error;
        }

        var added = RaiseChangeEvent(IdentityDomain.Events.OAuth2.Clients.SecretAdded(Id, secret2.Value, expiresOn));
        if (added.IsFailure)
        {
            return added.Error;
        }

        return new GeneratedClientSecret(secret, expiresOn);
    }

    public async Task<Result<Error>> RemoveLogoAsync(RemoveAvatarAction onRemoveOld)
    {
        if (!Logo.HasValue)
        {
            return Result.Ok;
        }

        var logoId = Logo.Value.ImageId;
        var removed = await onRemoveOld(logoId);
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        return RaiseChangeEvent(IdentityDomain.Events.OAuth2.Clients.LogoRemoved(Id, logoId));
    }

    public Result<Error> VerifySecret(string secret)
    {
        return Secrets.Verify(_passwordHasherService, secret);
    }
}

public record GeneratedClientSecret(string PlainSecret, Optional<DateTime> ExpiresOn);