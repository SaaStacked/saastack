using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Events.Shared.Organizations.Onboarding;
using Domain.Shared;
using Domain.Shared.Organizations;
using Created = Domain.Events.Shared.Organizations.Created;
using Deleted = Domain.Events.Shared.Organizations.Deleted;
using MembershipAdded = Domain.Events.Shared.Organizations.MembershipAdded;
using MembershipRemoved = Domain.Events.Shared.Organizations.MembershipRemoved;

namespace OrganizationsDomain;

public static class Events
{
    public static AvatarAdded AvatarAdded(Identifier id, Avatar avatar)
    {
        return new AvatarAdded(id)
        {
            AvatarId = avatar.ImageId,
            AvatarUrl = avatar.Url
        };
    }

    public static AvatarRemoved AvatarRemoved(Identifier id, Identifier avatarId)
    {
        return new AvatarRemoved(id)
        {
            AvatarId = avatarId
        };
    }

    public static BillingSubscribed BillingSubscribed(Identifier id, Identifier subscriptionId, Identifier subscriberId)
    {
        return new BillingSubscribed(id)
        {
            SubscriptionId = subscriptionId,
            SubscriberId = subscriberId
        };
    }

    public static BillingSubscriberChanged BillingSubscriberChanged(Identifier id, Identifier transfererId,
        Identifier transfereeId)
    {
        return new BillingSubscriberChanged(id)
        {
            FromSubscriberId = transfererId,
            ToSubscriberId = transfereeId
        };
    }

    public static Created Created(Identifier id, OrganizationOwnership ownership, Identifier createdById,
        Optional<EmailAddress> createdByEmailAddress, DisplayName name, DatacenterLocation hostRegion)
    {
        return new Created(id)
        {
            Name = name,
            Ownership = ownership,
            CreatedById = createdById,
            CreatedByEmailAddress = createdByEmailAddress.HasValue
                ? createdByEmailAddress.Value.Address
                : null,
            HostRegion = hostRegion.Code
        };
    }

    public static Deleted Deleted(Identifier id, Identifier deletedById)
    {
        return new Deleted(id, deletedById);
    }

    public static MemberInvited MemberInvited(Identifier id, Identifier invitedById, Optional<Identifier> invitedId,
        Optional<EmailAddress> userEmailAddress)
    {
        return new MemberInvited(id)
        {
            InvitedById = invitedById,
            InvitedId = invitedId.ValueOrDefault!,
            EmailAddress = userEmailAddress.ValueOrDefault?.Address
        };
    }

    public static MembershipAdded MembershipAdded(Identifier id, Identifier userId)
    {
        return new MembershipAdded(id)
        {
            UserId = userId
        };
    }

    public static MembershipRemoved MembershipRemoved(Identifier id, Identifier userId)
    {
        return new MembershipRemoved(id)
        {
            UserId = userId
        };
    }

    public static MemberUnInvited MemberUnInvited(Identifier id, Identifier uninvitedById, Identifier uninvitedId)
    {
        return new MemberUnInvited(id)
        {
            UninvitedById = uninvitedById,
            UninvitedId = uninvitedId
        };
    }

    public static NameChanged NameChanged(Identifier id, DisplayName name)
    {
        return new NameChanged(id)
        {
            Name = name
        };
    }

    public static OnboardingEnded OnboardingEnded(Identifier id, Identifier onboardingId)
    {
        return new OnboardingEnded(id)
        {
            OnboardingId = onboardingId
        };
    }

    public static OnboardingStarted OnboardingStarted(Identifier id, Identifier onboardingId)
    {
        return new OnboardingStarted(id)
        {
            OnboardingId = onboardingId
        };
    }

#if TESTINGONLY
    public static OnboardingReset OnboardingReset(Identifier id)
    {
        return new OnboardingReset(id);
    }
#endif

    public static RoleAssigned RoleAssigned(Identifier id, Identifier assignerId, Identifier userId, Role role)
    {
        return new RoleAssigned(id)
        {
            AssignedById = assignerId,
            UserId = userId,
            Role = role.Identifier
        };
    }

    public static RoleUnassigned RoleUnassigned(Identifier id, Identifier unassignerId, Identifier userId, Role role)
    {
        return new RoleUnassigned(id)
        {
            UnassignedById = unassignerId,
            UserId = userId,
            Role = role.Identifier
        };
    }

    public static SettingCreated SettingCreated(Identifier id, string name, string value, SettingValueType valueType,
        bool isEncrypted)
    {
        return new SettingCreated(id)
        {
            Name = name,
            StringValue = value,
            ValueType = valueType,
            IsEncrypted = isEncrypted
        };
    }

    public static SettingUpdated SettingUpdated(Identifier id, string name, string from, SettingValueType fromType,
        string to, SettingValueType toType, bool isEncrypted)
    {
        return new SettingUpdated(id)
        {
            Name = name,
            From = from,
            FromType = fromType,
            To = to,
            ToType = toType,
            IsEncrypted = isEncrypted
        };
    }

    public static class Onboarding
    {
        public static Completed Completed(Identifier id, Identifier organizationId, string completedBy)
        {
            return new Completed(id)
            {
                OrganizationId = organizationId,
                CompletedBy = completedBy
            };
        }

        public static Domain.Events.Shared.Organizations.Onboarding.Created Created(Identifier id,
            Identifier organizationId)
        {
            return new Domain.Events.Shared.Organizations.Onboarding.Created(id)
            {
                OrganizationId = organizationId
            };
        }

        public static StepStateChanged StepStateChanged(Identifier id,
            Identifier organizationId, string stepId, StringNameValues values)
        {
            return new StepStateChanged(id)
            {
                OrganizationId = organizationId,
                CurrentStepId = stepId,
                Values = new Dictionary<string, string>(values.Items)
            };
        }

        public static StepNavigated StepNavigated(Identifier id, Identifier organizationId, string fromStepId,
            string toStepId, Identifier navigatedById)
        {
            return new StepNavigated(id)
            {
                OrganizationId = organizationId,
                FromStepId = fromStepId,
                ToStepId = toStepId,
                NavigatedById = navigatedById
            };
        }

#if TESTINGONLY
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static Domain.Events.Shared.Organizations.Onboarding.Deleted Deleted(Identifier id,
            Identifier deletedById)
        {
            return new Domain.Events.Shared.Organizations.Onboarding.Deleted(id, deletedById);
        }
#endif
    }
}