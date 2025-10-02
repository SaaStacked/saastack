using System.Globalization;
using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.APIKeys;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using Domain.Shared.Identities;
using IdentityDomain.DomainServices;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class APIKeyRoot : AggregateRootBase
{
    private readonly IAPIKeyHasherService _apiKeyHasherService;

    public static Result<APIKeyRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        IAPIKeyHasherService apiKeyHasherService, Identifier userId, APIKeyToken keyToken)
    {
        var root = new APIKeyRoot(recorder, idFactory, apiKeyHasherService);
        root.RaiseCreateEvent(IdentityDomain.Events.APIKeys.Created(root.Id, userId, keyToken.Token,
            apiKeyHasherService.HashAPIKey(keyToken.Key)));
        return root;
    }

    private APIKeyRoot(IRecorder recorder, IIdentifierFactory idFactory, IAPIKeyHasherService apiKeyHasherService) :
        base(recorder, idFactory)
    {
        _apiKeyHasherService = apiKeyHasherService;
    }

    private APIKeyRoot(IRecorder recorder, IIdentifierFactory idFactory, IAPIKeyHasherService apiKeyHasherService,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
        _apiKeyHasherService = apiKeyHasherService;
    }

    public Optional<APIKeyKeep> ApiKey { get; private set; }

    public Optional<string> Description { get; private set; }

    public Optional<DateTime> ExpiresOn { get; private set; }

    public bool IsExpired => ExpiresOn.HasValue && ExpiresOn < DateTime.UtcNow;

    public bool IsRevoked => RevokedOn.HasValue && RevokedOn < DateTime.UtcNow;

    public bool IsStillValid => !IsExpired && !IsRevoked;

    public Optional<DateTime> RevokedOn { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    [UsedImplicitly]
    public static AggregateRootFactory<APIKeyRoot> Rehydrate()
    {
        return (identifier, container, _) => new APIKeyRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), container.GetRequiredService<IAPIKeyHasherService>(),
            identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                UserId = created.UserId.ToId();

                var apiKey = APIKeyKeep.Create(_apiKeyHasherService, created.KeyToken, created.KeyHash);
                if (apiKey.IsFailure)
                {
                    return apiKey.Error;
                }

                ApiKey = apiKey.Value;
                return Result.Ok;
            }

            case ParametersChanged changed:
            {
                Description = changed.Description;
                ExpiresOn = changed.ExpiresOn.ToOptional();
                var expiresInHours = changed.ExpiresOn.HasValue
                    ? DateTime.UtcNow.Subtract(changed.ExpiresOn.Value).TotalHours
                        .ToString(CultureInfo.InvariantCulture)
                    : "never";
                Recorder.TraceDebug(null, "ApiKey {Id} set its parameters, and expires in {ExpiresOn} hours", Id,
                    expiresInHours);
                return Result.Ok;
            }

            case KeyVerified _:
            {
                Recorder.TraceDebug(null, "ApiKey {Id} was verified", Id);
                return Result.Ok;
            }

            case Expired changed:
            {
                ExpiresOn = changed.ExpiredOn;
                Recorder.TraceDebug(null, "ApiKey {Id} was expired", Id);
                return Result.Ok;
            }

            case Revoked changed:
            {
                RevokedOn = changed.RevokedOn;
                Recorder.TraceDebug(null, "ApiKey {Id} was revoked", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> Delete(Identifier deleterId)
    {
        if (!IsOwner(deleterId))
        {
            return Error.RuleViolation(Resources.ApiKeyRoot_NotOwner);
        }

        return RaisePermanentDeleteEvent(IdentityDomain.Events.APIKeys.Deleted(Id, deleterId));
    }

    public Result<Error> ForceExpire(Identifier userId)
    {
        if (!IsOwner(userId))
        {
            return Error.RuleViolation(Resources.ApiKeyRoot_NotOwner);
        }

        if (IsExpired)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(IdentityDomain.Events.APIKeys.Expired(Id, UserId));
    }

    public Result<Error> Revoke(Roles revokerRoles)
    {
        if (!IsOperations(revokerRoles))
        {
            return Error.RuleViolation(Resources.ApiKeyRoot_NotOperator);
        }

        return RaiseChangeEvent(IdentityDomain.Events.APIKeys.Revoked(Id, UserId));
    }

    public Result<Error> SetParameters(string description, Optional<DateTime> expiresOn)
    {
        if (description.IsInvalidParameter(Validations.ApiKey.Description, nameof(description),
                Resources.ApiKeyKeep_InvalidDescription, out var error1))
        {
            return error1;
        }

        if (expiresOn.HasValue)
        {
            var lowerLimit = DateTime.UtcNow.ToNearestMinute().Add(Validations.ApiKey.MinimumExpiryPeriod);
            if (expiresOn.Value.IsInvalidParameter(
                    exp => exp == lowerLimit || exp.IsAfter(lowerLimit),
                    nameof(expiresOn), Resources.APIKeyRoot_ExpiresOnTooSoon, out var error2))
            {
                return error2;
            }
        }

        var nothingHasChanged = description.ToOptional() == Description
                                && expiresOn.ToOptional() == ExpiresOn;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(IdentityDomain.Events.APIKeys.ParametersChanged(Id, description, expiresOn));
    }

#if TESTINGONLY
    public void TestingOnly_Expire()
    {
        ExpiresOn = DateTime.UtcNow.SubtractSeconds(1);
    }
#endif

    public Result<bool, Error> VerifyKey(string key)
    {
        if (key.IsInvalidParameter(k => _apiKeyHasherService.ValidateKey(k), nameof(key),
                Resources.ApiKeyKeep_InvalidKey, out var error1))
        {
            return error1;
        }

        if (!ApiKey.HasValue)
        {
            return Error.RuleViolation(Resources.APIKeyRoot_Verify_NoApiKey);
        }

        if (!IsStillValid)
        {
            return false;
        }

        var verified = ApiKey.Value.Verify(_apiKeyHasherService, key);
        if (verified.IsFailure)
        {
            return verified.Error;
        }

        var isVerified = verified.Value;
        var raised = RaiseChangeEvent(IdentityDomain.Events.APIKeys.KeyVerified(Id, isVerified));
        if (raised.IsFailure)
        {
            return raised.Error;
        }

        return verified;
    }

    private static bool IsOperations(Roles roles)
    {
        return roles.HasRole(PlatformRoles.Operations);
    }

    private bool IsOwner(Identifier userId)
    {
        return UserId == userId;
    }
}