using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.UserProfiles;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using JetBrains.Annotations;

namespace UserProfilesDomain;

public sealed class UserProfileRoot : AggregateRootBase
{
    public static Result<UserProfileRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        ProfileType type, Identifier userId, PersonName name)
    {
        var root = new UserProfileRoot(recorder, idFactory);
        root.RaiseCreateEvent(UserProfilesDomain.Events.Created(root.Id, type, userId, name));
        return root;
    }

    private UserProfileRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private UserProfileRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Address Address { get; private set; } = Address.Default;

    public Optional<Avatar> Avatar { get; private set; }

    public Optional<Identifier> DefaultOrganizationId { get; private set; }

    public Optional<PersonDisplayName> DisplayName { get; private set; }

    public Optional<EmailAddress> EmailAddress { get; private set; }

    public Locale Locale { get; private set; } = Locale.Default;

    public Optional<PersonName> Name { get; private set; }

    public Optional<PhoneNumber> PhoneNumber { get; private set; }

    public Timezone Timezone { get; private set; } = Timezone.Default;

    public ProfileType Type { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    [UsedImplicitly]
    public static AggregateRootFactory<UserProfileRoot> Rehydrate()
    {
        return (identifier, container, _) => new UserProfileRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        if (!Name.HasValue || !DisplayName.HasValue)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_NotNamed);
        }

        if (EmailAddress.HasValue && Type != ProfileType.Person)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_PersonHasNoEmailAddress);
        }

        if (Type == ProfileType.Machine && EmailAddress.HasValue)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_MachineHasEmailAddress);
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
                Type = created.Type.ToEnumOrDefault(ProfileType.Person);
                var name = PersonName.Create(created.FirstName, created.LastName);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                Name = name.Value;

                var displayName = PersonDisplayName.Create(created.DisplayName);
                if (displayName.IsFailure)
                {
                    return displayName.Error;
                }

                DisplayName = displayName.Value;

                Address = Address.Default;
                EmailAddress = Optional<EmailAddress>.None;
                Timezone = Timezone.Default;
                Locale = Locale.Default;
                Avatar = Optional<Avatar>.None;
                return Result.Ok;
            }

            case EmailAddressChanged changed:
            {
                var email = Domain.Shared.EmailAddress.Create(changed.EmailAddress);
                if (email.IsFailure)
                {
                    return email.Error;
                }

                EmailAddress = email.Value;
                Recorder.TraceDebug(null, "Person {Id} changed email to {EmailAddress}", Id,
                    changed.EmailAddress);
                return Result.Ok;
            }

            case ContactAddressChanged changed:
            {
                var address = Address.Create(changed.Line1, changed.Line2, changed.Line3, changed.City, changed.State,
                    CountryCodes.FindOrDefault(changed.CountryCode), changed.Zip);
                if (address.IsFailure)
                {
                    return address.Error;
                }

                Address = address.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed address", Id);
                return Result.Ok;
            }

            case TimezoneChanged changed:
            {
                var timezone = Timezone.Create(changed.Timezone);
                if (timezone.IsFailure)
                {
                    return timezone.Error;
                }

                Timezone = timezone.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed timezone to {Timezone}", Id, changed.Timezone);
                return Result.Ok;
            }

            case LocaleChanged changed:
            {
                var locale = Locale.Create(changed.Locale);
                if (locale.IsFailure)
                {
                    return locale.Error;
                }

                Locale = locale.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed locale to {Locale}", Id, changed.Locale);
                return Result.Ok;
            }

            case NameChanged changed:
            {
                var name = PersonName.Create(changed.FirstName, changed.LastName);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                Name = name.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed name to {Name}", Id, name.Value.FullName);
                return Result.Ok;
            }

            case DisplayNameChanged changed:
            {
                var name = PersonDisplayName.Create(changed.DisplayName);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                DisplayName = name.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed display name to {DisplayName}", Id, name.Value);
                return Result.Ok;
            }

            case PhoneNumberChanged changed:
            {
                var number = Domain.Shared.PhoneNumber.Create(changed.Number);
                if (number.IsFailure)
                {
                    return number.Error;
                }

                PhoneNumber = number.Value;
                Recorder.TraceDebug(null, "Profile {Id} changed phone number to {PhoneNumber}", Id, number.Value);
                return Result.Ok;
            }

            case AvatarAdded added:
            {
                var avatar = Domain.Shared.Avatar.Create(added.AvatarId.ToId(), added.AvatarUrl);
                if (avatar.IsFailure)
                {
                    return avatar.Error;
                }

                Avatar = avatar.Value;
                Recorder.TraceDebug(null, "Profile {Id} added avatar {Image}", Id, avatar.Value.ImageId);
                return Result.Ok;
            }

            case AvatarRemoved _:
            {
                Avatar = Optional<Avatar>.None;
                Recorder.TraceDebug(null, "Profile {Id} removed avatar", Id);
                return Result.Ok;
            }

            case DefaultOrganizationChanged changed:
            {
                DefaultOrganizationId = changed.ToOrganizationId.ToId();
                Recorder.TraceDebug(null, "Profile {Id} changed its default organization to {OrganizationId}", Id,
                    changed.ToOrganizationId);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public async Task<Result<Error>> ChangeAvatarAsync(Identifier modifierId, CreateAvatarAction onCreateNew,
        RemoveAvatarAction onRemoveOld)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotOwner);
        }

        if (Type != ProfileType.Person)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_NotAPerson);
        }

        var existingAvatarId = Avatar.HasValue
            ? Avatar.Value.ImageId.ToOptional()
            : Optional<Identifier>.None;
        var created = await onCreateNew(Domain.Shared.Name.Create(DisplayName.Value.Text).Value);
        if (created.IsFailure)
        {
            return created.Error;
        }

        if (existingAvatarId.HasValue)
        {
            var removed = await onRemoveOld(existingAvatarId.Value);
            if (removed.IsFailure)
            {
                return removed.Error;
            }
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.AvatarAdded(Id, UserId, created.Value));
    }

    public Result<Error> ChangeDefaultOrganization(Identifier modifierId, Identifier organizationId)
    {
        var nothingHasChanged = organizationId.ToOptional() == DefaultOrganizationId;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(
            UserProfilesDomain.Events.DefaultOrganizationChanged(Id, UserId, DefaultOrganizationId, organizationId));
    }

    public Result<Error> ChangeDisplayName(Identifier modifierId, PersonDisplayName displayName)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotOwner);
        }

        var nothingHasChanged = displayName.ToOptional() == DisplayName;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.DisplayNameChanged(Id, UserId, displayName));
    }

    public Result<Error> ChangeLocale(Identifier modifierId, Locale locale)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotOwner);
        }

        var nothingHasChanged = locale == Locale;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.LocaleChanged(Id, UserId, locale));
    }

    public Result<Error> ChangeName(Identifier modifierId, PersonName name)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotOwner);
        }

        var nothingHasChanged = name.ToOptional() == Name;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.NameChanged(Id, UserId, name));
    }

    public Result<Error> ChangePhoneNumber(Identifier modifierId, PhoneNumber number)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotOwner);
        }

        if (Type != ProfileType.Person)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_NotAPerson);
        }

        var nothingHasChanged = number.ToOptional() == PhoneNumber;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.PhoneNumberChanged(Id, UserId, number));
    }

    public Result<Error> ChangeTimezone(Identifier modifierId, Timezone timezone)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotOwner);
        }

        var nothingHasChanged = timezone == Timezone;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.TimezoneChanged(Id, UserId, timezone));
    }

    public Result<Error> ForceRemoveAvatar(Identifier deleterId)
    {
        if (IsNotServiceAccount(deleterId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotServiceAccount);
        }

        if (Type != ProfileType.Person)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_NotAPerson);
        }

        if (!Avatar.HasValue)
        {
            return Result.Ok;
        }

        var avatarId = Avatar.Value.ImageId;
        return RaiseChangeEvent(UserProfilesDomain.Events.AvatarRemoved(Id, UserId, avatarId));
    }

    public async Task<Result<Error>> RemoveAvatarAsync(Identifier deleterId, RemoveAvatarAction onRemoveOld)
    {
        if (IsNotOwner(deleterId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotOwner);
        }

        if (Type != ProfileType.Person)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_NotAPerson);
        }

        if (!Avatar.HasValue)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_NoAvatar);
        }

        var avatarId = Avatar.Value.ImageId;
        var removed = await onRemoveOld(avatarId);
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.AvatarRemoved(Id, UserId, avatarId));
    }

    public Result<Error> SetContactAddress(Identifier modifierId, Address address)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotOwner);
        }

        var nothingHasChanged = address == Address;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.ContactAddressChanged(Id, UserId, address));
    }

    public Result<Error> SetEmailAddress(Identifier modifierId, EmailAddress emailAddress)
    {
        if (IsNotOwner(modifierId))
        {
            return Error.RoleViolation(Resources.UserProfileRoot_NotOwner);
        }

        if (Type != ProfileType.Person)
        {
            return Error.RuleViolation(Resources.UserProfileRoot_NotAPerson);
        }

        var nothingHasChanged = emailAddress == EmailAddress;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(UserProfilesDomain.Events.EmailAddressChanged(Id, UserId, emailAddress));
    }

#if TESTINGONLY

    public void TestingOnly_ChangeType(ProfileType type)
    {
        Type = type;
    }
#endif

    private bool IsNotOwner(Identifier modifierId)
    {
        return modifierId != UserId;
    }

    private static bool IsNotServiceAccount(Identifier modifierId)
    {
        return !CallerConstants.IsServiceAccount(modifierId);
    }
}