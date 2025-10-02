using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.EndUsers;
using Domain.Shared.Organizations;
using Domain.Shared.Subscriptions;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace EndUsersDomain.UnitTests;

[UsedImplicitly]
public class EndUserRootSpec
{
    private const string TestingToken = "Ll4qhv77XhiXSqsTUc6icu56ZLrqu5p1gH9kT5IlHio";

    private static EndUserRoot CreateOrgOwner(Mock<IRecorder> recorder, string organizationId,
        UserClassification classification = UserClassification.Person)
    {
        var owner = EndUserRoot.Create(recorder.Object, "anownerid".ToIdentifierFactory(), classification,
                DatacenterLocations.Local)
            .Value;
        owner.Register(Roles.Create(PlatformRoles.Standard).Value,
            Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
            EmailAddress.Create("orgowner@company.com").Value);
        owner.AddMembership(owner, OrganizationOwnership.Shared, organizationId.ToId(),
            Roles.Create(TenantRoles.Owner).Value, Features.Empty);

        return owner;
    }

    private static EndUserRoot CreateOrgMember(Mock<IRecorder> recorder, string organizationId)
    {
        var owner = EndUserRoot
            .Create(recorder.Object, "amemberid".ToIdentifierFactory(), UserClassification.Person,
                DatacenterLocations.Local)
            .Value;
        owner.Register(Roles.Create(PlatformRoles.Standard).Value,
            Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
            EmailAddress.Create("orgowner@company.com").Value);
        owner.AddMembership(owner, OrganizationOwnership.Shared, organizationId.ToId(),
            Roles.Create(TenantRoles.Member).Value, Features.Empty);

        return owner;
    }

    private static EndUserRoot CreateOperator(Mock<IRecorder> recorder, Mock<IIdentifierFactory> identifierFactory)
    {
        var @operator = EndUserRoot.Create(recorder.Object, identifierFactory.Object, UserClassification.Person,
                DatacenterLocations.Local)
            .Value;
        @operator.Register(Roles.Create(PlatformRoles.Standard, PlatformRoles.Operations).Value,
            Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
            EmailAddress.Create("operator@company.com").Value);

        return @operator;
    }

    [Trait("Category", "Unit")]
    public class GivenAPerson
    {
        private readonly Mock<IIdentifierFactory> _identifierFactory;
        private readonly Mock<IRecorder> _recorder;
        private readonly Mock<ITokensService> _tokensService;
        private readonly EndUserRoot _user;

        public GivenAPerson()
        {
            _recorder = new Mock<IRecorder>();
            var counter = 0;
            _identifierFactory = new Mock<IIdentifierFactory>();
            _identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
                .Returns((IIdentifiableEntity entity) =>
                {
                    if (entity is Membership)
                    {
                        return $"amembershipid{++counter}".ToId();
                    }

                    return "anid".ToId();
                });
            _tokensService = new Mock<ITokensService>();
            _tokensService.Setup(ts => ts.CreateGuestInvitationToken())
                .Returns(TestingToken);

            _user = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person,
                DatacenterLocations.Local).Value;
        }

        [Fact]
        public void WhenConstructed_ThenAssigned()
        {
            _user.Access.Should().Be(UserAccess.Enabled);
            _user.Status.Should().Be(UserStatus.Unregistered);
            _user.Classification.Should().Be(UserClassification.Person);
            _user.HostRegion.Should().Be(DatacenterLocations.Local);
            _user.Roles.HasNone().Should().BeTrue();
            _user.Features.HasNone().Should().BeTrue();
            _user.GuestInvitation.IsInvited.Should().BeFalse();
        }

        [Fact]
        public async Task WhenRegisterAndInvitedAsGuest_ThenAcceptsInvitationAndRegistered()
        {
            var emailAddress = EmailAddress.Create("auser@company.com").Value;
            var userProfile = EndUserProfile.Create("afirstname").Value;
            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) => Task.FromResult(Result.Ok));
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, userProfile, emailAddress);

            _user.Access.Should().Be(UserAccess.Enabled);
            _user.Status.Should().Be(UserStatus.Registered);
            _user.Classification.Should().Be(UserClassification.Person);
            _user.Roles.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard).Value);
            _user.Features.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic).Value);
            _user.GuestInvitation.IsAccepted.Should().BeTrue();
            _user.GuestInvitation.AcceptedEmailAddress.Should().Be(emailAddress);
            _user.Events[2].Should().BeOfType<GuestInvitationAccepted>();
            _user.Events.Last().Should().BeOfType<Registered>();
        }

        [Fact]
        public void WhenRegister_ThenRegistered()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);

            _user.Access.Should().Be(UserAccess.Enabled);
            _user.Status.Should().Be(UserStatus.Registered);
            _user.Classification.Should().Be(UserClassification.Person);
            _user.Roles.Items.Should().ContainInOrder(Role.Create(PlatformRoles.Standard).Value);
            _user.Features.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic).Value);
            _user.Events.Last().Should().BeOfType<Registered>();
        }

        [Fact]
        public void WhenEnsureInvariantsAndRegisteredPersonDoesNotHaveADefaultRole_ThenReturnsError()
        {
            _user.Register(Roles.Empty, Features.Create(PlatformFeatures.Basic).Value,
                EndUserProfile.Create("afirstname").Value, EmailAddress.Create("auser@company.com").Value);

            var result = _user.EnsureInvariants();

            result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_AllPersonsMustHaveDefaultRole);
        }

        [Fact]
        public void WhenEnsureInvariantsAndRegisteredPersonDoesNotHaveADefaultFeature_ThenReturnsError()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Empty, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);

            var result = _user.EnsureInvariants();

            result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_AllPersonsMustHaveDefaultFeature);
        }

        [Fact]
        public void WhenEnsureInvariantsAndRegisteredPersonStillInvited_ThenReturnsError()
        {
            var emailAddress = EmailAddress.Create("auser@company.com").Value;
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                emailAddress);
#if TESTINGONLY
            _user.TestingOnly_InviteGuest(emailAddress);
#endif

            var result = _user.EnsureInvariants();

            result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_GuestAlreadyRegistered);
        }

        [Fact]
        public void WhenAddMembershipByNonOwner_ThenReturnsError()
        {
            var inviter = CreateOrgMember(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);

            var result = _user.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Empty,
                Features.Empty);

            result.Should().BeError(ErrorCode.RoleViolation, Resources.EndUserRoot_NotOrganizationOwner);
        }

        [Fact]
        public void WhenAddMembershipByOtherToPersonalOrganization_ThenReturnsError()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var inviter = CreateOrgOwner(_recorder, "anorganizationid");

            var result = _user.AddMembership(inviter, OrganizationOwnership.Personal, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value, Features.Create(TenantFeatures.Basic).Value);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_AddMembership_SharedOwnershipRequired);
        }

        [Fact]
        public void WhenAddMembershipByOtherToSharedOrganization_ThenAddsMembership()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var inviter = CreateOrgOwner(_recorder, "anorganizationid");
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeSuccess();
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

        [Fact]
        public void WhenAddMembershipAndAlreadyMember_ThenReturns()
        {
            var inviter = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(), Roles.Empty,
                Features.Empty);

            var result = _user.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Empty,
                Features.Empty);

            result.Should().BeSuccess();
        }

        [Fact]
        public void WhenAddMembershipAndAlreadyPersonalOrg_ThenReturnsError()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(_user, OrganizationOwnership.Personal, "anorganizationid1".ToId(), Roles.Empty,
                Features.Empty);

            var result = _user.AddMembership(_user, OrganizationOwnership.Personal, "anorganizationid2".ToId(),
                Roles.Create(TenantRoles.Owner).Value, Features.Empty);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_AddMembership_OnlyOnePersonalOrganization);
        }

        [Fact]
        public void WhenAddMembershipToMachinesPersonalOrganization_ThenReturnsError()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var inviter = CreateOrgOwner(_recorder, "anorganizationid", UserClassification.Machine);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(inviter, OrganizationOwnership.Personal, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_AddMembership_SharedOwnershipRequired);
        }

        [Fact]
        public void WhenAddMembershipToMachinesSharedOrganization_ThenAddsMembership()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var inviter = CreateOrgOwner(_recorder, "anorganizationid", UserClassification.Machine);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeSuccess();
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

        [Fact]
        public void WhenAddMembershipBySelfToPersonalOrganization_ThenAddsMembership()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(_user, OrganizationOwnership.Personal, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeSuccess();
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

        [Fact]
        public void WhenAddMembershipBySelfToSharedOrganization_ThenAddsMembership()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(_user, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeSuccess();
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

        [Fact]
        public void WhenAddMembership_ThenAddsMembershipAsDefaultWithRolesAndFeatures()
        {
            var inviter = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(), roles,
                features);

            result.Should().BeSuccess();
            _user.Memberships.Should().OnlyContain(ms =>
                ms.OrganizationId.Value == "anorganizationid"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

        [Fact]
        public void WhenAddMembershipAndAlreadyHasMembership_ThenChangesToDefaultMembership()
        {
            var inviter = CreateOrgOwner(_recorder, "anorganizationid2");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;
            _user.AddMembership(_user, OrganizationOwnership.Shared, "anorganizationid1".ToId(), roles, features);

            var result = _user.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid2".ToId(), roles,
                features);

            result.Should().BeSuccess();
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid1"
                && !ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid2"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

#if TESTINGONLY
        [Fact]
        public void WhenAssignMembershipFeaturesAndNoMembership_ThenReturnsError()
        {
            var result = _user.AssignMembershipFeatures("anassignerid".ToId(), "anorganizationid".ToId(),
                Features.Create(TenantFeatures.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_NoMembership.Format("anorganizationid"));
        }
#endif

        [Fact]
        public void WhenAssignMembershipFeaturesAndFeatureNotAssignable_ThenReturnsError()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.AssignMembershipFeatures("anassignerid".ToId(), "anorganizationid".ToId(),
                Features.Create("anunknownfeature").Value, (_, _, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_UnassignableTenantFeature.Format("anunknownfeature"));
        }

#if TESTINGONLY
        [Fact]
        public void WhenAssignMembershipFeaturesAndHasFeature_ThenDoesNothing()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic, TenantFeatures.TestingOnly).Value);

            var result = _user.AssignMembershipFeatures("anassignerid".ToId(), "anorganizationid".ToId(),
                Features.Create(TenantFeatures.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Memberships[0].Roles.Should()
                .Be(Roles.Create(TenantRoles.Member).Value);
            _user.Memberships[0].Features.Should()
                .Be(Features.Create(TenantFeatures.Basic, TenantFeatures.TestingOnly).Value);
            _user.Events.Should().NotContainItemsAssignableTo<MembershipFeatureAssigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenAssignMembershipFeatures_ThenAssigns()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.AssignMembershipFeatures("anassignerid".ToId(), "anorganizationid".ToId(),
                Features.Create(TenantFeatures.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Memberships[0].Roles.Should().Be(Roles.Create(TenantRoles.Member).Value);
            _user.Memberships[0].Features.Should()
                .Be(Features.Create(TenantFeatures.Basic, TenantFeatures.TestingOnly).Value);
            _user.Events.Last().Should().BeOfType<MembershipFeatureAssigned>();
        }
#endif

        [Fact]
        public void WhenUnassignMembershipFeaturesAndFeatureNotAssignable_ThenReturnsError()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.UnassignMembershipFeatures("anunassignerid".ToId(), "anorganizationid".ToId(),
                Features.Create("anunknownfeature").Value, (_, _, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_UnassignableTenantFeature.Format("anunknownfeature"));
        }

#if TESTINGONLY
        [Fact]
        public void WhenUnassignMembershipFeaturesAndNotHaveFeature_ThenDoesNothing()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.UnassignMembershipFeatures("anunassignerid".ToId(), "anorganizationid".ToId(),
                Features.Create(TenantFeatures.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Memberships[0].Roles.Should()
                .Be(Roles.Create(TenantRoles.Member).Value);
            _user.Memberships[0].Features.Should()
                .Be(Features.Create(TenantFeatures.Basic).Value);
            _user.Events.Should().NotContainItemsAssignableTo<MembershipFeatureUnassigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenUnassignMembershipFeatures_ThenUnassigns()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic, TenantFeatures.TestingOnly).Value);

            var result = _user.UnassignMembershipFeatures("anunassignerid".ToId(), "anorganizationid".ToId(),
                Features.Create(TenantFeatures.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Memberships[0].Roles.Should()
                .Be(Roles.Create(TenantRoles.Member).Value);
            _user.Memberships[0].Features.Should()
                .Be(Features.Create(TenantFeatures.Basic).Value);
            _user.Events.Last().Should().BeOfType<MembershipFeatureUnassigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenAssignMembershipRolesAndAssignerNotOwner_ThenReturnsError()
        {
            var assigner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person,
                    DatacenterLocations.Local)
                .Value;

            var result = _user.AssignMembershipRoles(assigner, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RoleViolation, Resources.EndUserRoot_NotOrganizationOwner);
        }
#endif

        [Fact]
        public void WhenAssignMembershipRolesAndRoleNotAssignable_ThenReturnsError()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.AssignMembershipRoles(assigner, "anorganizationid".ToId(),
                Roles.Create("anunknownrole").Value, (_, _, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_UnassignableTenantRole.Format("anunknownrole"));
        }

#if TESTINGONLY
        [Fact]
        public void WhenAssignMembershipRolesAndAlreadyHasRole_ThenDoesNothing()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member, TenantRoles.TestingOnly).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.AssignMembershipRoles(assigner, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Memberships[0].Roles.Should()
                .Be(Roles.Create(TenantRoles.Member, TenantRoles.TestingOnly).Value);
            _user.Memberships[0].Features.Should()
                .Be(Features.Create(TenantFeatures.Basic).Value);
            _user.Events.Should().NotContainItemsAssignableTo<MembershipRoleAssigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenAssignMembershipRoles_ThenAssigns()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.AssignMembershipRoles(assigner, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Memberships[0].Roles.Should()
                .Be(Roles.Create(TenantRoles.Member, TenantRoles.TestingOnly).Value);
            _user.Memberships[0].Features.Should()
                .Be(Features.Create(TenantFeatures.Basic).Value);
            _user.Events.Last().Should().BeOfType<MembershipRoleAssigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenUnassignMembershipRolesAndAssignerNotOwner_ThenReturnsError()
        {
            var assigner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person,
                    DatacenterLocations.Local)
                .Value;

            var result = _user.UnassignMembershipRoles(assigner, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RoleViolation, Resources.EndUserRoot_NotOrganizationOwner);
        }
#endif

        [Fact]
        public void WhenUnassignMembershipRolesAndRoleNotAssignable_ThenReturnsError()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.UnassignMembershipRoles(assigner, "anorganizationid".ToId(),
                Roles.Create("anunknownrole").Value, (_, _, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_UnassignableTenantRole.Format("anunknownrole"));
        }

#if TESTINGONLY
        [Fact]
        public void WhenUnassignMembershipRolesAndNotHaveRole_ThenDoesNothing()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.UnassignMembershipRoles(assigner, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Memberships[0].Roles.Should()
                .Be(Roles.Create(TenantRoles.Member).Value);
            _user.Memberships[0].Features.Should()
                .Be(Features.Create(TenantFeatures.Basic).Value);
            _user.Events.Should().NotContainItemsAssignableTo<MembershipRoleUnassigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenUnassignMembershipRoles_ThenUnassigns()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member, TenantRoles.TestingOnly).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.UnassignMembershipRoles(assigner, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.TestingOnly).Value, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Memberships[0].Roles.Should()
                .Be(Roles.Create(TenantRoles.Member).Value);
            _user.Memberships[0].Features.Should()
                .Be(Features.Create(TenantFeatures.Basic).Value);
            _user.Events.Last().Should().BeOfType<MembershipRoleUnassigned>();
        }
#endif

        [Fact]
        public void WhenAssignPlatformFeaturesAndFeatureNotAssignable_ThenReturnsError()
        {
            var result = _user.AssignPlatformFeatures("anassignerid".ToId(), Features.Create("anunknownfeature").Value,
                (_, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_UnassignablePlatformFeature.Format("anunknownfeature"));
        }

#if TESTINGONLY
        [Fact]
        public void WhenAssignPlatformFeaturesAndHasFeature_ThenDoesNothing()
        {
            _user.AssignPlatformFeatures("anassignerid".ToId(), Features.Create(PlatformFeatures.TestingOnly).Value,
                (_, _, _) => Result.Ok);

            var result = _user.AssignPlatformFeatures("anassignerid".ToId(),
                Features.Create(PlatformFeatures.TestingOnly).Value, (_, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Roles.HasNone().Should().BeTrue();
            _user.Features.Should().Be(Features.Create(PlatformFeatures.TestingOnly).Value);
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenAssignPlatformFeatures_ThenAssigns()
        {
            var result = _user.AssignPlatformFeatures("anassignerid".ToId(),
                Features.Create(PlatformFeatures.TestingOnly).Value, (_, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Roles.HasNone().Should().BeTrue();
            _user.Features.Should().Be(Features.Create(PlatformFeatures.TestingOnly).Value);
            _user.Events.Last().Should().BeOfType<PlatformFeatureAssigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenAssignPlatformRolesAndAssignerNotOperator_ThenReturnsError()
        {
            var assigner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person,
                    DatacenterLocations.Local)
                .Value;

            var result = _user.AssignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value,
                (_, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_NotOperator);
        }
#endif

        [Fact]
        public void WhenAssignPlatformRolesAndRoleNotAssignable_ThenReturnsError()
        {
            var assigner = CreateOperator(_recorder, _identifierFactory);

            var result =
                _user.AssignPlatformRoles(assigner, Roles.Create("anunknownrole").Value, (_, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_UnassignablePlatformRole.Format("anunknownrole"));
        }

#if TESTINGONLY
        [Fact]
        public void WhenAssignPlatformRolesAndHasRole_ThenDoesNothing()
        {
            var assigner = CreateOperator(_recorder, _identifierFactory);
            _user.AssignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value, (_, _, _) => Result.Ok);

            var result = _user.AssignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value,
                (_, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Roles.Should().Be(Roles.Create(PlatformRoles.TestingOnly).Value);
            _user.Features.HasNone().Should().BeTrue();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenAssignPlatformRoles_ThenAssigns()
        {
            var assigner = CreateOperator(_recorder, _identifierFactory);

            var result = _user.AssignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value,
                (_, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Roles.Should().Be(Roles.Create(PlatformRoles.TestingOnly).Value);
            _user.Features.HasNone().Should().BeTrue();
            _user.Events.Last().Should().BeOfType<PlatformRoleAssigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenUnassignPlatformRolesAndAssignerNotOperator_ThenReturnsError()
        {
            var assigner = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person,
                    DatacenterLocations.Local)
                .Value;

            var result = _user.UnassignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value,
                (_, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_NotOperator);
        }
#endif

        [Fact]
        public void WhenUnassignPlatformRolesAndRoleNotAssignable_ThenReturnsError()
        {
            var assigner = CreateOperator(_recorder, _identifierFactory);

            var result =
                _user.UnassignPlatformRoles(assigner, Roles.Create("anunknownrole").Value, (_, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_UnassignablePlatformRole.Format("anunknownrole"));
        }

#if TESTINGONLY
        [Fact]
        public void WhenUnassignPlatformRolesAndUserNotAssignedRole_ThenDoesNothing()
        {
            var assigner = CreateOperator(_recorder, _identifierFactory);

            var result = _user.UnassignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value,
                (_, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Events.Should().NotContainItemsAssignableTo<PlatformRoleUnassigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenUnassignPlatformRolesAndStandardRole_ThenReturnsError()
        {
            var assigner = CreateOperator(_recorder, _identifierFactory);

            var result = _user.UnassignPlatformRoles(assigner, Roles.Create(PlatformRoles.Standard).Value,
                (_, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_CannotUnassignBaselinePlatformRole.Format(PlatformRoles.Standard));
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenUnassignPlatformRoles_ThenUnassigns()
        {
            var assigner = CreateOperator(_recorder, _identifierFactory);
            _user.AssignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value, (_, _, _) => Result.Ok);

            var result = _user.UnassignPlatformRoles(assigner, Roles.Create(PlatformRoles.TestingOnly).Value,
                (_, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Roles.HasNone().Should().BeTrue();
            _user.Features.HasNone().Should().BeTrue();
            _user.Events.Last().Should().BeOfType<PlatformRoleUnassigned>();
        }
#endif

        [Fact]
        public void WhenUnassignPlatformFeaturesAndFeatureNotAssignable_ThenReturnsError()
        {
            var result = _user.UnassignPlatformFeatures("anunassignerid".ToId(),
                Features.Create("anunknownfeature").Value, (_, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_UnassignablePlatformFeature.Format("anunknownfeature"));
        }

#if TESTINGONLY
        [Fact]
        public void WhenUnassignPlatformFeaturesAndUserNotAssignedFeature_ThenDoesNothing()
        {
            var result = _user.UnassignPlatformFeatures("anunassignerid".ToId(),
                Features.Create(PlatformFeatures.TestingOnly).Value, (_, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Events.Should().NotContainItemsAssignableTo<PlatformFeatureUnassigned>();
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenUnassignPlatformFeaturesAndStandardFeature_ThenReturnsError()
        {
            var result = _user.UnassignPlatformFeatures("anunassignerid".ToId(),
                Features.Create(PlatformFeatures.Basic).Value, (_, _, _) => Result.Ok);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_CannotUnassignBaselinePlatformFeature.Format(PlatformFeatures.Basic));
        }
#endif

#if TESTINGONLY
        [Fact]
        public void WhenUnassignPlatformFeatures_ThenUnassigns()
        {
            _user.AssignPlatformFeatures("anassignerid".ToId(), Features.Create(PlatformFeatures.TestingOnly).Value,
                (_, _, _) => Result.Ok);

            var result = _user.UnassignPlatformFeatures("anunassignerid".ToId(),
                Features.Create(PlatformFeatures.TestingOnly).Value, (_, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Roles.HasNone().Should().BeTrue();
            _user.Features.HasNone().Should().BeTrue();
            _user.Events.Last().Should().BeOfType<PlatformFeatureUnassigned>();
        }
#endif

        [Fact]
        public async Task WhenInviteAsGuestAndRegistered_ThenDoesNothing()
        {
            var emailAddress = EmailAddress.Create("invitee@company.com").Value;
            _user.Register(Roles.Empty, Features.Empty, EndUserProfile.Create("afirstname").Value, emailAddress);
            var wasCallbackCalled = false;

            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) =>
                {
                    wasCallbackCalled = true;
                    return Task.FromResult(Result.Ok);
                });

            wasCallbackCalled.Should().BeFalse();
            _user.Events.Last().Should().BeOfType<Created>();
            _user.GuestInvitation.IsInvited.Should().BeFalse();
            _user.GuestInvitation.IsAccepted.Should().BeFalse();
        }

        [Fact]
        public async Task WhenInviteAsGuestAndAlreadyInvited_ThenInvitedAgain()
        {
            var emailAddress = EmailAddress.Create("invitee@company.com").Value;
            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) => Task.FromResult(Result.Ok));
            var wasCallbackCalled = false;

            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) =>
                {
                    wasCallbackCalled = true;
                    return Task.FromResult(Result.Ok);
                });

            wasCallbackCalled.Should().BeTrue();
            _user.Events.Last().Should().BeOfType<GuestInvitationCreated>();
            _user.GuestInvitation.IsInvited.Should().BeTrue();
            _user.GuestInvitation.IsAccepted.Should().BeFalse();
        }

        [Fact]
        public async Task WhenInviteAsGuestAndUnknown_ThenInvited()
        {
            var emailAddress = EmailAddress.Create("invitee@company.com").Value;
            var wasCallbackCalled = false;

            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) =>
                {
                    wasCallbackCalled = true;
                    return Task.FromResult(Result.Ok);
                });

            wasCallbackCalled.Should().BeTrue();
            _user.Events.Last().Should().BeOfType<GuestInvitationCreated>();
            _user.GuestInvitation.IsInvited.Should().BeTrue();
            _user.GuestInvitation.IsAccepted.Should().BeFalse();
        }

        [Fact]
        public async Task WhenReInviteGuestAsyncAndNotInvited_ThenReturnsError()
        {
            var wasCallbackCalled = false;

            var result = await _user.ReInviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
                (_, _) =>
                {
                    wasCallbackCalled = true;
                    return Task.FromResult(Result.Ok);
                });

            wasCallbackCalled.Should().BeFalse();
            result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_GuestInvitationNeverSent);
        }

        [Fact]
        public async Task WhenReInviteGuestAsyncAndInvitationExpired_ThenReturnsError()
        {
            var emailAddress = EmailAddress.Create("invitee@company.com").Value;
            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) => Task.FromResult(Result.Ok));
#if TESTINGONLY
            _user.TestingOnly_ExpireGuestInvitation();
#endif
            var wasCallbackCalled = false;

            var result = await _user.ReInviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
                (_, _) =>
                {
                    wasCallbackCalled = true;
                    return Task.FromResult(Result.Ok);
                });

            wasCallbackCalled.Should().BeFalse();
            result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_GuestInvitationHasExpired);
        }

        [Fact]
        public async Task WhenReInviteGuestAsyncAndInvited_ThenReInvites()
        {
            var emailAddress = EmailAddress.Create("invitee@company.com").Value;
            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) => Task.FromResult(Result.Ok));
            var wasCallbackCalled = false;

            await _user.ReInviteGuestAsync(_tokensService.Object, "aninviterid".ToId(),
                (_, _) =>
                {
                    wasCallbackCalled = true;
                    return Task.FromResult(Result.Ok);
                });

            wasCallbackCalled.Should().BeTrue();
            _user.Events.Last().Should().BeOfType<GuestInvitationCreated>();
            _user.GuestInvitation.IsInvited.Should().BeTrue();
            _user.GuestInvitation.IsAccepted.Should().BeFalse();
        }

        [Fact]
        public void WhenVerifyGuestInvitationAndAlreadyRegistered_ThenReturnsError()
        {
            var emailAddress = EmailAddress.Create("invitee@company.com").Value;
            _user.Register(Roles.Empty, Features.Empty, EndUserProfile.Create("afirstname").Value, emailAddress);

            var result = _user.VerifyGuestInvitation();

            result.Should().BeError(ErrorCode.EntityExists, Resources.EndUserRoot_GuestAlreadyRegistered);
        }

        [Fact]
        public void WhenVerifyGuestInvitationAndNotInvited_ThenReturnsError()
        {
            var result = _user.VerifyGuestInvitation();

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.EndUserRoot_GuestInvitationNeverSent);
        }

        [Fact]
        public async Task WhenVerifyGuestInvitationAndInvitationExpired_ThenReturnsError()
        {
            var emailAddress = EmailAddress.Create("invitee@company.com").Value;
            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) => Task.FromResult(Result.Ok));
#if TESTINGONLY
            _user.TestingOnly_ExpireGuestInvitation();
#endif

            var result = _user.VerifyGuestInvitation();

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.EndUserRoot_GuestInvitationHasExpired);
        }

        [Fact]
        public async Task WhenVerifyGuestInvitationAndStillValid_ThenVerifies()
        {
            var emailAddress = EmailAddress.Create("invitee@company.com").Value;
            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) => Task.FromResult(Result.Ok));

            var result = _user.VerifyGuestInvitation();

            result.Should().BeSuccess();
        }

        [Fact]
        public void WhenAcceptGuestInvitationAndAuthenticatedUser_ThenReturnsError()
        {
            var emailAddress = EmailAddress.Create("auser@company.com").Value;

            var result = _user.AcceptGuestInvitation("auserid".ToId(), emailAddress);

            result.Should().BeError(ErrorCode.ForbiddenAccess,
                Resources.EndUserRoot_GuestInvitationAcceptedByNonAnonymousUser);
        }

        [Fact]
        public void WhenAcceptGuestInvitationAndRegistered_ThenReturnsError()
        {
            var emailAddress = EmailAddress.Create("auser@company.com").Value;
            _user.Register(Roles.Empty, Features.Empty, EndUserProfile.Create("afirstname").Value, emailAddress);

            var result = _user.AcceptGuestInvitation(CallerConstants.AnonymousUserId.ToId(), emailAddress);

            result.Should().BeError(ErrorCode.EntityExists, Resources.EndUserRoot_GuestAlreadyRegistered);
        }

        [Fact]
        public void WhenAcceptGuestInvitationAndNotInvited_ThenReturnsError()
        {
            var emailAddress = EmailAddress.Create("auser@company.com").Value;

            var result = _user.AcceptGuestInvitation(CallerConstants.AnonymousUserId.ToId(), emailAddress);

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.EndUserRoot_GuestInvitationNeverSent);
        }

        [Fact]
        public async Task WhenAcceptGuestInvitationAndInviteExpired_ThenReturnsError()
        {
            var emailAddress = EmailAddress.Create("auser@company.com").Value;
            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) => Task.FromResult(Result.Ok));
#if TESTINGONLY
            _user.TestingOnly_ExpireGuestInvitation();
#endif

            var result = _user.AcceptGuestInvitation(CallerConstants.AnonymousUserId.ToId(), emailAddress);

            result.Should().BeError(ErrorCode.PreconditionViolation, Resources.EndUserRoot_GuestInvitationHasExpired);
        }

        [Fact]
        public async Task WhenAcceptGuestInvitation_ThenAccepts()
        {
            var emailAddress = EmailAddress.Create("auser@company.com").Value;
            await _user.InviteGuestAsync(_tokensService.Object, "aninviterid".ToId(), emailAddress,
                (_, _) => Task.FromResult(Result.Ok));

            var result = _user.AcceptGuestInvitation(CallerConstants.AnonymousUserId.ToId(), emailAddress);

            result.Should().BeSuccess();
            _user.Events.Last().Should().BeOfType<GuestInvitationAccepted>();
            _user.GuestInvitation.IsAccepted.Should().BeTrue();
            _user.GuestInvitation.AcceptedEmailAddress.Should().Be(emailAddress);
        }

        [Fact]
        public void WhenChangeDefaultMembershipAndNotAMember_ThenReturnsError()
        {
            var result = _user.ChangeDefaultMembership("anorganizationid".ToId());

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_NoMembership.Format("anorganizationid"));
        }

        [Fact]
        public void WhenChangeDefaultMembershipAndAlreadyDefault_ThenReturns()
        {
            _user.AddMembership(_user, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value, Features.Create(TenantFeatures.Basic).Value);

            var result = _user.ChangeDefaultMembership("anorganizationid".ToId());

            result.Should().BeSuccess();
            _user.Memberships.DefaultMembership.OrganizationId.Value.Should().Be("anorganizationid".ToId());
        }

        [Fact]
        public void WhenChangeDefaultMembership_ThenChangesDefault()
        {
            _user.AddMembership(_user, OrganizationOwnership.Shared, "anorganizationid1".ToId(),
                Roles.Create(TenantRoles.Member).Value, Features.Create(TenantFeatures.Basic).Value);
            _user.AddMembership(_user, OrganizationOwnership.Shared, "anorganizationid2".ToId(),
                Roles.Create(TenantRoles.Member).Value, Features.Create(TenantFeatures.Basic).Value);

            var result = _user.ChangeDefaultMembership("anorganizationid1".ToId());

            result.Should().BeSuccess();
            _user.Memberships.DefaultMembership.OrganizationId.Value.Should().Be("anorganizationid1".ToId());
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

        [Fact]
        public void WhenRemoveMembershipAndNotOwner_ThenReturnsError()
        {
            var result = _user.RemoveMembership(_user, "anorganizationid".ToId());

            result.Should().BeError(ErrorCode.RoleViolation, Resources.EndUserRoot_NotOrganizationOwner);
        }

        [Fact]
        public void WhenRemoveMembershipAndNotMember_ThenDoesNothing()
        {
            var uninviter = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person,
                    DatacenterLocations.Local)
                .Value;
            uninviter.AddMembership(uninviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Owner).Value, Features.Create(TenantFeatures.Basic).Value);

            var result = _user.RemoveMembership(uninviter, "anorganizationid".ToId());

            result.Should().BeSuccess();
            _user.Memberships.HasNone().Should().BeTrue();
            _user.Events.Last().Should().BeOfType<Created>();
        }

        [Fact]
        public void WhenRemoveMembershipAndIsNotDefaultMembership_ThenRemoves()
        {
            var uninviter = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person,
                    DatacenterLocations.Local)
                .Value;
            uninviter.AddMembership(uninviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Owner).Value, Features.Create(TenantFeatures.Basic).Value);
            _user.AddMembership(uninviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value, Features.Create(TenantFeatures.Basic).Value);

            var result = _user.RemoveMembership(uninviter, "anorganizationid".ToId());

            result.Should().BeSuccess();
            _user.Memberships.HasNone().Should().BeTrue();
            _user.Events.Count.Should().Be(4);
            _user.Events[0].Should().BeOfType<Created>();
            _user.Events[1].Should().BeOfType<MembershipAdded>();
            _user.Events[2].Should().BeOfType<DefaultMembershipChanged>();
            _user.Events.Last().Should().BeOfType<MembershipRemoved>();
        }

        [Fact]
        public void WhenRemoveMembershipAndIsDefaultMembership_ThenRemovesAndResetsDefault()
        {
            var uninviter = EndUserRoot.Create(_recorder.Object, _identifierFactory.Object, UserClassification.Person,
                    DatacenterLocations.Local)
                .Value;
            uninviter.AddMembership(uninviter, OrganizationOwnership.Shared, "anorganizationid2".ToId(),
                Roles.Create(TenantRoles.Owner).Value, Features.Create(TenantFeatures.Basic).Value);
            _user.AddMembership(_user, OrganizationOwnership.Personal, "anorganizationid1".ToId(),
                Roles.Create(TenantRoles.Owner).Value, Features.Create(TenantFeatures.Basic).Value);
            _user.AddMembership(uninviter, OrganizationOwnership.Shared, "anorganizationid2".ToId(),
                Roles.Create(TenantRoles.Member).Value, Features.Create(TenantFeatures.Basic).Value);

            var result = _user.RemoveMembership(uninviter, "anorganizationid2".ToId());

            result.Should().BeSuccess();
            _user.Memberships.Count.Should().Be(1);
            _user.Events.Count.Should().Be(7);
            _user.Events[0].Should().BeOfType<Created>();
            _user.Events[1].Should().BeOfType<MembershipAdded>();
            _user.Events[2].Should().BeOfType<DefaultMembershipChanged>();
            _user.Events[3].Should().BeOfType<MembershipAdded>();
            _user.Events[4].Should().BeOfType<DefaultMembershipChanged>();
            _user.Events[5].Should().BeOfType<DefaultMembershipChanged>();
            _user.Events.Last().Should().BeOfType<MembershipRemoved>();
        }

        [Fact]
        public void WhenResetMembershipFeaturesFromEnterpriseToUnsubscribed_ThenUnassigns()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Paid3).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Paid3).Value);

            var result = _user.ResetMembershipFeatures(CallerConstants.MaintenanceAccountUserId.ToId(),
                "anorganizationid".ToId(), BillingSubscriptionTier.Unsubscribed, "aplanid",
                (_, _, _) => Result.Ok, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Features.Items.Should().OnlyContain(feat => feat == Feature.Create(PlatformFeatures.Basic).Value);
            _user.Memberships[0].Features.Items.Should()
                .OnlyContain(feat => feat == Feature.Create(TenantFeatures.Basic).Value);
            _user.Events.Last().Should().BeOfType<MembershipFeaturesReset>();
        }

        [Fact]
        public void WhenResetMembershipFeaturesFromBasicToEnterprise_ThenAssigns()
        {
            var assigner = CreateOrgOwner(_recorder, "anorganizationid");
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            _user.AddMembership(assigner, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value,
                Features.Create(TenantFeatures.Basic).Value);

            var result = _user.ResetMembershipFeatures(CallerConstants.MaintenanceAccountUserId.ToId(),
                "anorganizationid".ToId(), BillingSubscriptionTier.Enterprise, "aplanid",
                (_, _, _) => Result.Ok, (_, _, _, _) => Result.Ok);

            result.Should().BeSuccess();
            _user.Features.Items.Should().OnlyContain(feat => feat == Feature.Create(PlatformFeatures.Paid3).Value);
            _user.Memberships[0].Features.Items.Should()
                .OnlyContain(feat => feat == Feature.Create(TenantFeatures.Paid3).Value);
            _user.Events.Last().Should().BeOfType<MembershipFeaturesReset>();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAMachine
    {
        private readonly Mock<IRecorder> _recorder;
        private readonly EndUserRoot _user;

        public GivenAMachine()
        {
            _recorder = new Mock<IRecorder>();
            var counter = 0;
            var identifierFactory = new Mock<IIdentifierFactory>();
            identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
                .Returns((IIdentifiableEntity entity) =>
                {
                    if (entity is Membership)
                    {
                        return $"amembershipid{++counter}".ToId();
                    }

                    return "anid".ToId();
                });
            var tokensService = new Mock<ITokensService>();
            tokensService.Setup(ts => ts.CreateGuestInvitationToken())
                .Returns("aninvitationtoken");

            _user = EndUserRoot.Create(_recorder.Object, identifierFactory.Object, UserClassification.Machine,
                DatacenterLocations.Local).Value;
        }

        [Fact]
        public void WhenConstructed_ThenAssigned()
        {
            _user.Access.Should().Be(UserAccess.Enabled);
            _user.Status.Should().Be(UserStatus.Unregistered);
            _user.Classification.Should().Be(UserClassification.Machine);
            _user.HostRegion.Should().Be(DatacenterLocations.Local);
            _user.Roles.HasNone().Should().BeTrue();
            _user.Features.HasNone().Should().BeTrue();
            _user.GuestInvitation.IsInvited.Should().BeFalse();
        }

        [Fact]
        public void WhenEnsureInvariantsAndMachineIsNotRegistered_ThenReturnsError()
        {
            var result = _user.EnsureInvariants();

            result.Should().BeError(ErrorCode.RuleViolation, Resources.EndUserRoot_MachineNotRegistered);
        }

        [Fact]
        public void WhenAddMembershipByOtherToPersonsSharedOrganization_ThenAddsMembership()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var inviter = CreateOrgOwner(_recorder, "anorganizationid");
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeSuccess();
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

        [Fact]
        public void WhenAddMembershipByOtherToPersonsPersonalOrganization_ThenReturnsError()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var inviter = CreateOrgOwner(_recorder, "anorganizationid");

            var result = _user.AddMembership(inviter, OrganizationOwnership.Personal, "anorganizationid".ToId(),
                Roles.Create(TenantRoles.Member).Value, Features.Create(TenantFeatures.Basic).Value);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_AddMembership_SharedOwnershipRequired);
        }

        [Fact]
        public void WhenAddMembershipToMachinesPersonalOrganization_ThenReturnsError()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var inviter = CreateOrgOwner(_recorder, "anorganizationid", UserClassification.Machine);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(inviter, OrganizationOwnership.Personal, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeError(ErrorCode.RuleViolation,
                Resources.EndUserRoot_AddMembership_SharedOwnershipRequired);
        }

        [Fact]
        public void WhenAddMembershipToMachinesSharedOrganization_ThenAddsMembership()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var inviter = CreateOrgOwner(_recorder, "anorganizationid", UserClassification.Machine);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(inviter, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeSuccess();
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

        [Fact]
        public void WhenAddMembershipBySelfToPersonalOrganization_ThenAddsMembership()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(_user, OrganizationOwnership.Personal, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeSuccess();
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }

        [Fact]
        public void WhenAddMembershipBySelfToSharedOrganization_ThenAddsMembership()
        {
            _user.Register(Roles.Create(PlatformRoles.Standard).Value,
                Features.Create(PlatformFeatures.Basic).Value, EndUserProfile.Create("afirstname").Value,
                EmailAddress.Create("auser@company.com").Value);
            var roles = Roles.Create(TenantRoles.Member).Value;
            var features = Features.Create(TenantFeatures.Basic).Value;

            var result = _user.AddMembership(_user, OrganizationOwnership.Shared, "anorganizationid".ToId(),
                roles, features);

            result.Should().BeSuccess();
            _user.Memberships.Should().Contain(ms =>
                ms.OrganizationId.Value == "anorganizationid"
                && ms.IsDefault
                && ms.Roles == roles
                && ms.Features == features);
            _user.Events.Last().Should().BeOfType<DefaultMembershipChanged>();
        }
    }
}