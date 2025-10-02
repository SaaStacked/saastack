using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Shared;
using Domain.Shared.EndUsers;
using Domain.Shared.Organizations;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class OrganizationRootSpec
{
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly OrganizationRoot _org;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ITenantSettingService> _tenantSettingService;

    public OrganizationRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity _) => "anid".ToId());
        _tenantSettingService = new Mock<ITenantSettingService>();
        _tenantSettingService.Setup(tss => tss.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _tenantSettingService.Setup(tss => tss.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);

        _org = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object, _tenantSettingService.Object,
            OrganizationOwnership.Shared, "acreatorid".ToId(), UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
    }

    [Fact]
    public void WhenCreateWithMachineUser_ThenReturnsError()
    {
        var result = OrganizationRoot.Create(new Mock<IRecorder>().Object, new Mock<IIdentifierFactory>().Object,
            new Mock<ITenantSettingService>().Object, OrganizationOwnership.Shared, "acreatorid".ToId(),
            UserClassification.Machine, DisplayName.Create("aname").Value, DatacenterLocations.Local);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OrganizationRoot_Create_SharedRequiresPerson);
    }

    [Fact]
    public void WhenCreate_ThenAssigns()
    {
        _org.Name.Name.Should().Be("aname");
        _org.CreatedById.Should().Be("acreatorid".ToId());
        _org.BillingSubscriber.Should().Be(Optional<BillingSubscriber>.None);
        _org.Ownership.Should().Be(OrganizationOwnership.Shared);
        _org.Settings.Should().Be(Settings.Empty);
        _org.HostRegion.Should().Be(DatacenterLocations.Local);
    }

    [Fact]
    public void WhenCreateSettings_ThenAddsSettings()
    {
        _org.CreateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("avalue1", true).Value },
            { "aname2", Setting.Create("avalue2", true).Value }
        }).Value);

        _org.Settings.Properties.Count.Should().Be(2);
        _org.Settings.Properties["aname1"].Should().Be(Setting.Create("avalue1", true).Value);
        _org.Settings.Properties["aname2"].Should().Be(Setting.Create("avalue2", true).Value);
        _org.Events.Last().Should().BeOfType<SettingCreated>();
    }

    [Fact]
    public void WhenUpdateSettings_ThenAddsAndUpdatesSettings()
    {
        _org.CreateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("anoldvalue1", false).Value },
            { "aname2", Setting.Create("anoldvalue2", false).Value }
        }).Value);
        _org.UpdateSettings(Settings.Create(new Dictionary<string, Setting>
        {
            { "aname1", Setting.Create("anewvalue1", true).Value },
            { "aname3", Setting.Create("anewvalue3", true).Value }
        }).Value);

        _org.Settings.Properties.Count.Should().Be(3);
        _org.Settings.Properties["aname1"].Should().Be(Setting.Create("anewvalue1", true).Value);
        _org.Settings.Properties["aname2"].Should().Be(Setting.Create("anoldvalue2", false).Value);
        _org.Settings.Properties["aname3"].Should().Be(Setting.Create("anewvalue3", true).Value);
        _org.Events[3].Should().BeOfType<SettingUpdated>();
        _org.Events.Last().Should().BeOfType<SettingCreated>();
    }

    [Fact]
    public void WhenInviteMemberAndInviterNotOwner_ThenReturnsError()
    {
        var result = _org.InviteMember("aninviterid".ToId(), Roles.Empty, Optional<Identifier>.None,
            Optional<EmailAddress>.None);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotOrgOwner);
    }

    [Fact]
    public void WhenInviteMemberAndNoUser_ThenReturnsError()
    {
        var result = _org.InviteMember("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            Optional<Identifier>.None, Optional<EmailAddress>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.OrganizationRoot_InviteMember_UserIdAndEmailMissing);
    }

    [Fact]
    public void WhenInviteMemberAndNotShared_ThenReturnsError()
    {
#if TESTINGONLY
        _org.TestingOnly_ChangeOwnership(OrganizationOwnership.Personal);
#endif
        var result = _org.InviteMember("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            Optional<Identifier>.None, Optional<EmailAddress>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.OrganizationRoot_InviteMember_PersonalOrgMembershipNotAllowed);
    }

    [Fact]
    public void WhenInviteMemberWithUserId_ThenAddsMembership()
    {
        var result = _org.InviteMember("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            "auserid".ToId(), Optional<EmailAddress>.None);

        result.Should().BeSuccess();
        _org.Events.Last().Should().BeOfType<MemberInvited>();
    }

    [Fact]
    public void WhenInviteMemberWithEmailAddress_ThenAddsMembership()
    {
        var result = _org.InviteMember("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            Optional<Identifier>.None, EmailAddress.Create("auser@company.com").Value);

        result.Should().BeSuccess();
        _org.Events.Last().Should().BeOfType<MemberInvited>();
    }

    [Fact]
    public void WhenUnInviteMemberAndRemoverNotOwner_ThenReturnsError()
    {
        var result = _org.UnInviteMember("aremoverid".ToId(), Roles.Empty, "auserid".ToId());

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotOrgOwner);
    }

    [Fact]
    public void WhenUnInviteMemberAndBillingSubscriber_ThenReturnsError()
    {
        _org.SubscribeBilling("asubscriptionid".ToId(), "asubscriberid".ToId());

        var result = _org.UnInviteMember("aremoverid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            "asubscriberid".ToId());

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UninviteMember_BillingSubscriber);
    }

    [Fact]
    public void WhenUnInviteMemberAndPersonalOrg_ThenReturnsError()
    {
        var org = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, OrganizationOwnership.Personal, "acreatorid".ToId(),
            UserClassification.Person, DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;

        var result = org.UnInviteMember("aremoverid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OrganizationRoot_UnInviteMember_PersonalOrg);
    }

    [Fact]
    public void WhenUnInviteMemberAndNotMember_ThenDoesNothing()
    {
        _org.InviteMember("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId(),
            Optional<EmailAddress>.None);

        var result =
            _org.UnInviteMember("aremoverid".ToId(), Roles.Create(TenantRoles.Owner).Value, "anotheruserid".ToId());

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenUnInviteMemberAndIsMember_ThenRemovesMember()
    {
        _org.InviteMember("aninviterid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId(),
            Optional<EmailAddress>.None);

        var result =
            _org.UnInviteMember("aremoverid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId());

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        var result = await _org.ChangeAvatarAsync("anotheruserid".ToId(), Roles.Empty,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("animageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotOrgOwner);
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndNoExistingAvatar_ThenChanges()
    {
        Identifier? imageDeletedId = null;
        var result = await _org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("animageid".ToId(), "aurl").Value),
            id =>
            {
                imageDeletedId = id;
                return Task.FromResult(Result.Ok);
            });

        imageDeletedId.Should().BeNull();
        result.Should().BeSuccess();
        _org.Avatar.Value.ImageId.Should().Be("animageid".ToId());
        _org.Avatar.Value.Url.Should().Be("aurl");
        _org.Events.Last().Should().BeOfType<AvatarAdded>();
    }

    [Fact]
    public async Task WhenChangeAvatarAsyncAndHasExistingAvatar_ThenChanges()
    {
        Identifier? imageDeletedId = null;
        await _org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        var result = await _org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("animageid".ToId(), "aurl").Value),
            id =>
            {
                imageDeletedId = id;
                return Task.FromResult(Result.Ok);
            });

        imageDeletedId.Should().Be("anoldimageid".ToId());
        result.Should().BeSuccess();
        _org.Avatar.Value.ImageId.Should().Be("animageid".ToId());
        _org.Avatar.Value.Url.Should().Be("aurl");
        _org.Events.Last().Should().BeOfType<AvatarAdded>();
    }

    [Fact]
    public async Task WhenRemoveAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        var result = await _org.RemoveAvatarAsync("anotheruserid".ToId(), Roles.Empty, _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotOrgOwner);
    }

    [Fact]
    public async Task WhenRemoveAvatarAsyncAndNoExistingAvatar_ThenReturnsError()
    {
        var result = await _org.RemoveAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult(Result.Ok));

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OrganizationRoot_NoAvatar);
    }

    [Fact]
    public async Task WhenRemoveAvatarAsyncAndHasExistingAvatar_ThenRemoves()
    {
        Identifier? imageDeletedId = null;
        await _org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        var result = await _org.RemoveAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value, id =>
        {
            imageDeletedId = id;
            return Task.FromResult(Result.Ok);
        });

        imageDeletedId.Should().Be("anoldimageid".ToId());
        result.Should().BeSuccess();
        _org.Avatar.HasValue.Should().BeFalse();
        _org.Events.Last().Should().BeOfType<AvatarRemoved>();
    }

    [Fact]
    public void WhenForceRemoveAvatarAsyncAndNotOwner_ThenReturnsError()
    {
        var result = _org.ForceRemoveAvatar("anotheruserid".ToId());

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotServiceAccount);
    }

    [Fact]
    public async Task WhenForceRemoveAvatarByServiceAccountAndHasExistingAvatar_ThenRemoves()
    {
        await _org.ChangeAvatarAsync("auserid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            _ => Task.FromResult<Result<Avatar, Error>>(Avatar.Create("anoldimageid".ToId(), "aurl").Value),
            _ => Task.FromResult(Result.Ok));

        var result = _org.ForceRemoveAvatar(CallerConstants.ServiceClientAccountUserId.ToId());

        result.Should().BeSuccess();
        _org.Avatar.HasValue.Should().BeFalse();
        _org.Events.Last().Should().BeOfType<AvatarRemoved>();
    }

    [Fact]
    public void WhenAddMembershipAndExists_ThenDoesNothing()
    {
        _org.AddMembership("auserid".ToId());

        var result = _org.AddMembership("auserid".ToId());

        result.Should().BeSuccess();
        _org.Memberships.Count.Should().Be(1);
        _org.Memberships.Members[0].UserId.Should().Be("auserid".ToId());
        _org.Events.Last().Should().BeOfType<MembershipAdded>();
    }

    [Fact]
    public void WhenAddMembership_ThenAdded()
    {
        var result = _org.AddMembership("auserid".ToId());

        result.Should().BeSuccess();
        _org.Memberships.Count.Should().Be(1);
        _org.Memberships.Members[0].UserId.Should().Be("auserid".ToId());
        _org.Events.Last().Should().BeOfType<MembershipAdded>();
    }

    [Fact]
    public void WhenRemoveMembershipAndNoTExist_ThenDoesNothing()
    {
        var result = _org.RemoveMembership("auserid".ToId());

        result.Should().BeSuccess();
        _org.Memberships.Count.Should().Be(0);
        _org.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenRemoveMembership_ThenRemoves()
    {
        _org.AddMembership("auserid".ToId());

        var result = _org.RemoveMembership("auserid".ToId());

        result.Should().BeSuccess();
        _org.Memberships.Count.Should().Be(0);
        _org.Events.Last().Should().BeOfType<MembershipRemoved>();
    }

    [Fact]
    public void WhenAssignRolesAndNotOwner_ThenReturnsError()
    {
        var result = _org.AssignRoles("anassignerid".ToId(), Roles.Empty, "auserid".ToId(), Roles.Empty);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotOrgOwner);
    }

    [Fact]
    public void WhenAssignRolesAndNotMember_ThenReturnsError()
    {
        var result = _org.AssignRoles("anassignerid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId(),
            Roles.Empty);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OrganizationRoot_UserNotMember);
    }

    [Fact]
    public void WhenAssignRolesAndNotAssignableRole_ThenReturnsError()
    {
        _org.AddMembership("auserid".ToId());

        var result = _org.AssignRoles("anassignerid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId(),
            Roles.Create("arole").Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OrganizationRoot_RoleNotAssignable.Format("arole"));
    }

    [Fact]
    public void WhenAssignRoles_ThenAssigns()
    {
        _org.AddMembership("auserid".ToId());

        var result = _org.AssignRoles("anassignerid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId(),
            Roles.Create(TenantRoles.BillingAdmin).Value);

        result.Should().BeSuccess();
        _org.Events.Last().Should().BeOfType<RoleAssigned>();
    }

    [Fact]
    public void WhenUnassignRolesAndNotOwner_ThenReturnsError()
    {
        var result = _org.UnassignRoles("anassignerid".ToId(), Roles.Empty, "auserid".ToId(), Roles.Empty);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotOrgOwner);
    }

    [Fact]
    public void WhenUnassignRolesAndNotMember_ThenReturnsError()
    {
        var result = _org.UnassignRoles("anassignerid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId(),
            Roles.Empty);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OrganizationRoot_UserNotMember);
    }

    [Fact]
    public void WhenUnassignRolesAndNotAssignableRole_ThenReturnsError()
    {
        _org.AddMembership("auserid".ToId());

        var result = _org.UnassignRoles("anassignerid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId(),
            Roles.Create("arole").Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OrganizationRoot_RoleNotAssignable.Format("arole"));
    }

    [Fact]
    public void WhenUnassignRoles_ThenUnassigns()
    {
        _org.AddMembership("auserid".ToId());

        var result = _org.UnassignRoles("anassignerid".ToId(), Roles.Create(TenantRoles.Owner).Value, "auserid".ToId(),
            Roles.Create(TenantRoles.Owner).Value);

        result.Should().BeSuccess();
        _org.Events.Last().Should().BeOfType<RoleUnassigned>();
    }

    [Fact]
    public async Task WhenDeleteOrganizationAndNotOwner_ThenReturnsError()
    {
        var result = await _org.DeleteOrganizationAsync("adeleterid".ToId(), Roles.Empty, true,
            _ => Task.FromResult(Result.Ok), CancellationToken.None);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotOrgOwner);
    }

    [Fact]
    public async Task WhenDeleteOrganizationAndNotBillingSubscriber_ThenReturnsError()
    {
        _org.SubscribeBilling("asubscriptionid".ToId(), "asubscriberid".ToId());

        var result = await _org.DeleteOrganizationAsync("adeleterid".ToId(),
            Roles.Create(TenantRoles.BillingAdmin).Value, true, _ => Task.FromResult(Result.Ok),
            CancellationToken.None);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotBillingSubscriber);
    }

    [Fact]
    public async Task WhenDeleteOrganizationAndHasOtherMembers_ThenReturnsError()
    {
        _org.AddMembership("auserid1".ToId());
        _org.SubscribeBilling("asubscriptionid".ToId(), "asubscriberid".ToId());

        var result = await _org.DeleteOrganizationAsync("asubscriberid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            true, _ => Task.FromResult(Result.Ok), CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.OrganizationRoot_DeleteOrganization_MembersStillExist);
    }

    [Fact]
    public async Task WhenDeleteOrganizationAndCannotBeUnsubscribed_ThenReturnsError()
    {
        _org.SubscribeBilling("asubscriptionid".ToId(), "asubscriberid".ToId());

        var result = await _org.DeleteOrganizationAsync("asubscriberid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            false, _ => Task.FromResult(Result.Ok), CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.OrganizationRoot_DeleteOrganization_BillingSubscriptionCannotBeUnsubscribed);
    }

    [Fact]
    public async Task WhenDeleteOrganization_ThenDeletes()
    {
        _org.SubscribeBilling("asubscriptionid".ToId(), "asubscriberid".ToId());

        var result = await _org.DeleteOrganizationAsync("asubscriberid".ToId(), Roles.Create(TenantRoles.Owner).Value,
            true, _ => Task.FromResult(Result.Ok), CancellationToken.None);

        result.Should().BeSuccess();
        _org.IsDeleted.Value.Should().BeTrue();
    }

    [Fact]
    public void WhenTransferBillingSubscriberByOther_ThenReturnsError()
    {
        var result = _org.TransferBillingSubscriber("atransfererid".ToId(), "atransfereeid".ToId());

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationRoot_UserNotBillingSubscriber);
    }

    [Fact]
    public void WhenTransferBillingSubscriberByBillingSubscriber_ThenTransfers()
    {
        _org.SubscribeBilling("asubscriptionid".ToId(), "asubscriberid".ToId());

        var result = _org.TransferBillingSubscriber("asubscriberid".ToId(), "atransfereeid".ToId());

        result.Should().BeSuccess();
        _org.BillingSubscriber.Value.SubscriberId.Should().Be("atransfereeid".ToId());
        _org.Events.Last().Should().BeOfType<BillingSubscriberChanged>();
    }

    [Fact]
    public void WhenCanCancelBillingSubscriptionAndNotBillingSubscriber_ThenDenies()
    {
        var result = _org.CanCancelBillingSubscription("acancellerid".ToId(), Roles.Empty);

        result.Value.IsDenied.Should().BeTrue();
        result.Value.DisallowedReason.Should().Be(Resources.OrganizationRoot_UserNotBillingSubscriber);
    }

    [Fact]
    public void WhenCanCancelBillingSubscriptionAndBillingSubscriber_ThenAllows()
    {
        _org.SubscribeBilling("asubscriptionid".ToId(), "acancellerid".ToId());

        var result = _org.CanCancelBillingSubscription("acancellerid".ToId(), Roles.Empty);

        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void WhenCanChangeBillingSubscriptionPlanAndNotBillingSubscriber_ThenDenies()
    {
        var result = _org.CanChangeBillingSubscriptionPlan("amodifierid".ToId(), Roles.Empty);

        result.Value.IsDenied.Should().BeTrue();
        result.Value.DisallowedReason.Should().Be(Resources.OrganizationRoot_NotBillingSubscriberNorBillingAdmin);
    }

    [Fact]
    public void WhenCanChangeBillingSubscriptionPlanAndBillingAdmin_ThenAllows()
    {
        var result =
            _org.CanChangeBillingSubscriptionPlan("amodifierid".ToId(), Roles.Create(TenantRoles.BillingAdmin).Value);

        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void WhenCanChangeBillingSubscriptionPlanAndBillingSubscriber_ThenAllows()
    {
        _org.TransferBillingSubscriber("acreatorid".ToId(), "amodifierid".ToId());

        var result =
            _org.CanChangeBillingSubscriptionPlan("amodifierid".ToId(), Roles.Create(TenantRoles.BillingAdmin).Value);

        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void WhenCanTransferBillingSubscriberAndNotBillingSubscriber_ThenDenies()
    {
        var result = _org.CanTransferBillingSubscription("atransfererid".ToId(), "atransfereeid".ToId(), Roles.Empty);

        result.Value.IsDenied.Should().BeTrue();
        result.Value.DisallowedReason.Should().Be(Resources.OrganizationRoot_UserNotBillingSubscriber);
    }

    [Fact]
    public void WhenCanTransferBillingSubscriberAndTransfereeNotBillingAdmin_ThenDenies()
    {
        _org.SubscribeBilling("asubscriptionid".ToId(), "asubscriberid".ToId());

        var result = _org.CanTransferBillingSubscription("asubscriberid".ToId(), "atransfereeid".ToId(), Roles.Empty);

        result.Value.IsDenied.Should().BeTrue();
        result.Value.DisallowedReason.Should()
            .Be(Resources.OrganizationRoot_CanTransferBillingSubscription_TransfereeNotBillingAdmin);
    }

    [Fact]
    public void WhenCanTransferBillingSubscriberAndTransfereeIsBillingAdmin_ThenAllows()
    {
        _org.SubscribeBilling("asubscriptionid".ToId(), "asubscriberid".ToId());

        var result = _org.CanTransferBillingSubscription("asubscriberid".ToId(), "atransfereeid".ToId(),
            Roles.Create(TenantRoles.BillingAdmin).Value);

        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void WhenCanUnsubscribeSubscriptionAndNotBillingSubscriber_ThenDenies()
    {
        var result = _org.CanUnsubscribeBillingSubscription("anunsubscriberid".ToId());

        result.Value.IsDenied.Should().BeTrue();
        result.Value.DisallowedReason.Should().Be(Resources.OrganizationRoot_UserNotBillingSubscriber);
    }

    [Fact]
    public void WhenCanUnsubscribeBillingSubscription_ThenAllows()
    {
        _org.SubscribeBilling("asubscriptionid".ToId(), "anunsubscriberid".ToId());

        var result = _org.CanUnsubscribeBillingSubscription("anunsubscriberid".ToId());

        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void WhenCanViewBillingSubscriptionAndNotBillingSubscriber_ThenDenies()
    {
        var result = _org.CanViewBillingSubscription("aviewerid".ToId(), Roles.Empty);

        result.Value.IsDenied.Should().BeTrue();
        result.Value.DisallowedReason.Should().Be(Resources.OrganizationRoot_NotBillingSubscriberNorBillingAdmin);
    }

    [Fact]
    public void WhenCanViewBillingSubscriptionAndBillingAdmin_ThenAllows()
    {
        var result =
            _org.CanViewBillingSubscription("aviewerid".ToId(), Roles.Create(TenantRoles.BillingAdmin).Value);

        result.Value.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void WhenCanViewBillingSubscriptionAndBillingSubscriber_ThenAllows()
    {
        _org.TransferBillingSubscriber("acreatorid".ToId(), "aviewerid".ToId());

        var result =
            _org.CanViewBillingSubscription("aviewerid".ToId(), Roles.Create(TenantRoles.BillingAdmin).Value);

        result.Value.IsAllowed.Should().BeTrue();
    }
}