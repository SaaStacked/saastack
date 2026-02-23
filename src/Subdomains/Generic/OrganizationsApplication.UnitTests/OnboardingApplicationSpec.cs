using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Shared;
using Domain.Shared.EndUsers;
using FluentAssertions;
using Moq;
using OrganizationsApplication.ApplicationServices;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using OrganizationsDomain.DomainServices;
using UnitTesting.Common;
using Xunit;
using OrganizationOwnership = Domain.Shared.Organizations.OrganizationOwnership;
using PersonName = Application.Resources.Shared.PersonName;

namespace OrganizationsApplication.UnitTests;

[Trait("Category", "Unit")]
public class OnboardingApplicationSpec
{
    private readonly OnboardingApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IOrganizationEmailDomainService> _emailDomainService;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IOnboardingRepository> _onboardingRepository;
    private readonly Mock<IOrganizationRepository> _organizationRepository;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ITenantSettingService> _tenantSettingService;
    private readonly Mock<IUserProfilesService> _userProfilesService;
    private readonly Mock<IOnboardingWorkflowService> _workflowService;

    public OnboardingApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _caller.Setup(c => c.CallerId).Returns("acallerid");
        _caller.Setup(c => c.Roles).Returns(new ICallerContext.CallerRoles([], [TenantRoles.Owner]));
        _recorder = new Mock<IRecorder>();
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _identifierFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _tenantSettingService = new Mock<ITenantSettingService>();
        _tenantSettingService.Setup(tss => tss.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _tenantSettingService.Setup(tss => tss.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _emailDomainService = new Mock<IOrganizationEmailDomainService>();
        _emailDomainService.Setup(eds =>
                eds.EnsureUniqueAsync(It.IsAny<string>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _organizationRepository = new Mock<IOrganizationRepository>();
        _organizationRepository.Setup(rep =>
                rep.SaveAsync(It.IsAny<OrganizationRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationRoot root, CancellationToken _) => root);
        _onboardingRepository = new Mock<IOnboardingRepository>();
        _onboardingRepository.Setup(rep =>
                rep.SaveAsync(It.IsAny<OrganizationOnboardingRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationOnboardingRoot root, CancellationToken _) => root);
        _onboardingRepository.Setup(rep =>
                rep.SaveAsync(It.IsAny<OrganizationOnboardingRoot>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationOnboardingRoot root, bool _, CancellationToken _) => root);
        _userProfilesService = new Mock<IUserProfilesService>();
        var onboardingService = new Mock<ICustomOnboardingWorkflowService>();
        onboardingService.Setup(os => os.SaveWorkflowAsync(It.IsAny<string>(),
                It.IsAny<OrganizationOnboardingWorkflowSchema>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, OrganizationOnboardingWorkflowSchema workflow, CancellationToken _) => workflow);
        _workflowService = new Mock<IOnboardingWorkflowService>();
        _workflowService.Setup(ws =>
                ws.CalculateShortestPath(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Empty);

        _application = new OnboardingApplication(_recorder.Object, _identifierFactory.Object, onboardingService.Object,
            _workflowService.Object, _userProfilesService.Object, _organizationRepository.Object,
            _onboardingRepository.Object);
    }

    [Fact]
    public async Task WhenGetOnboardingAsyncAndOnboardingNotFound_ThenReturnsError()
    {
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OrganizationOnboardingRoot>.None);

        var result = await _application.GetOnboardingAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenGetOnboardingAsync_ThenReturnsOnboarding()
    {
        var workflow = CreateSimpleTwoStepWorkflow();
        _workflowService.Setup(ws => ws.FindWorkflow(It.IsAny<Identifier>()))
            .Returns(workflow);
        var onboarding = OrganizationOnboardingRoot.Create(_recorder.Object, _identifierFactory.Object,
            _workflowService.Object, "anorganizationid".ToId()).Value;
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboarding.ToOptional());

        var result = await _application.GetOnboardingAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.OrganizationId.Should().Be("anorganizationid");
    }

    [Fact]
    public async Task WhenInitiateOnboardingAsync_ThenStatsOnboarding()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, _emailDomainService.Object, OrganizationOwnership.Personal,
            "acreatorid".ToId(), EmailAddress.Create("auser@company.com").Value, UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        _organizationRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _organizationRepository.Setup(rep =>
                rep.SaveAsync(It.IsAny<OrganizationRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationRoot root, CancellationToken _) => root);
        var workflowSchema = CreateSimpleTwoStepWorkflowSchema();
        var workflow = CreateSimpleTwoStepWorkflow();
        _workflowService.Setup(ws => ws.FindWorkflow(It.IsAny<Identifier>()))
            .Returns(workflow);
        _workflowService.Setup(ws =>
                ws.CalculateShortestPath(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create(workflow.Journeys.Steps.Select(s => s.Value.ToStep().Value).ToList()).Value);
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "acallerid",
                UserId = "auserid",
                Name = new PersonName
                {
                    FirstName = "afirstname"
                },
                DisplayName = "adisplayname",
                EmailAddress = new UserProfileEmailAddress
                {
                    Address = "auser@company.com",
                    Classification = UserProfileEmailAddressClassification.Company
                }
            });

        var result = await _application.InitiateOnboardingAsync(_caller.Object, "anorganizationid", workflowSchema,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.OrganizationId.Should().Be("anorganizationid");
        result.Value.Workflow.Name.Should().Be("aname");
        result.Value.State!.CurrentStep.Id.Should().Be("astartstepid");
        result.Value.State!.PathTaken.Should().BeEmpty();
        result.Value.State.PathAhead.Count.Should().Be(3);
        result.Value.State.PathAhead[0].Id.Should().Be("astepid1");
        result.Value.State.PathAhead[1].Id.Should().Be("astepid2");
        result.Value.State.PathAhead[2].Id.Should().Be("anendstepid");
        _organizationRepository.Verify(rep =>
            rep.SaveAsync(It.Is<OrganizationRoot>(root =>
                root.OnboardingStatus == OnboardingStatus.InProgress
            ), It.IsAny<CancellationToken>()));
        _onboardingRepository.Verify(rep =>
            rep.SaveAsync(It.Is<OrganizationOnboardingRoot>(root =>
                root.OrganizationId == "anorganizationid".ToId()
                && root.State.Status == OnboardingStatus.InProgress
                && root.State.CurrentStepId == "astartstepid"), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenMoveForwardAsyncAndOnboardingNotFound_ThenReturnsError()
    {
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OrganizationOnboardingRoot>.None);

        var result = await _application.MoveForwardAsync(_caller.Object, "anorganizationid", "anextst epid", null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenMoveForwardAsyncWithStepValues_ThenMovesForward()
    {
        var workflow = CreateSimpleTwoStepWorkflow();
        _workflowService.Setup(ws => ws.FindWorkflow(It.IsAny<Identifier>()))
            .Returns(workflow);
        _workflowService.Setup(ws =>
                ws.CalculateShortestPathToEnd(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create(workflow.Journeys.Steps.Select(s => s.Value.ToStep().Value).Skip(2).ToList())
                .Value);
        var onboarding = OrganizationOnboardingRoot.Create(_recorder.Object, _identifierFactory.Object,
            _workflowService.Object, "anorganizationid".ToId()).Value;
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboarding.ToOptional());
        var stepValues = new Dictionary<string, string> { { "aname", "avalue" } };

        var result = await _application.MoveForwardAsync(_caller.Object, "anorganizationid", "astepid1",
            stepValues, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.State!.CurrentStep.Id.Should().Be("astepid1");
        result.Value.State!.PathTaken.Should().OnlyContain(s => s.Id == "astartstepid");
        result.Value.State.PathAhead.Count.Should().Be(2);
        result.Value.State.PathAhead[0].Id.Should().Be("astepid2");
        result.Value.State.PathAhead[1].Id.Should().Be("anendstepid");
        _onboardingRepository.Verify(rep =>
            rep.SaveAsync(It.Is<OrganizationOnboardingRoot>(root =>
                root.OrganizationId == "anorganizationid".ToId()
                && root.State.Status == OnboardingStatus.InProgress
                && root.State.CurrentStepId == "astepid1"
                && root.State.AllValues.Items["aname"] == "avalue"
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenMoveForwardAsyncWithoutStepValues_ThenMovesForward()
    {
        var workflow = CreateSimpleTwoStepWorkflow();
        _workflowService.Setup(ws => ws.FindWorkflow(It.IsAny<Identifier>()))
            .Returns(workflow);
        _workflowService.Setup(ws =>
                ws.CalculateShortestPathToEnd(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create(workflow.Journeys.Steps.Select(s => s.Value.ToStep().Value).Skip(2).ToList())
                .Value);
        var onboarding = OrganizationOnboardingRoot.Create(_recorder.Object, _identifierFactory.Object,
            _workflowService.Object, "anorganizationid".ToId()).Value;
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboarding.ToOptional());

        var result = await _application.MoveForwardAsync(_caller.Object, "anorganizationid", "astepid1",
            null, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.State!.CurrentStep.Id.Should().Be("astepid1");
        result.Value.State!.PathTaken.Should().OnlyContain(s => s.Id == "astartstepid");
        result.Value.State.PathAhead.Count.Should().Be(2);
        result.Value.State.PathAhead[0].Id.Should().Be("astepid2");
        result.Value.State.PathAhead[1].Id.Should().Be("anendstepid");
        _onboardingRepository.Verify(rep =>
            rep.SaveAsync(It.Is<OrganizationOnboardingRoot>(root =>
                root.OrganizationId == "anorganizationid".ToId()
                && root.State.Status == OnboardingStatus.InProgress
                && root.State.CurrentStepId == "astepid1"
                && root.State.AllValues.Items.Count == 0
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenMoveBackwardAsyncAndOnboardingNotFound_ThenReturnsError()
    {
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OrganizationOnboardingRoot>.None);

        var result = await _application.MoveBackwardAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenMoveBackwardAsync_ThenMovesBackward()
    {
        var workflow = CreateSimpleTwoStepWorkflow();
        _workflowService.Setup(ws => ws.FindWorkflow(It.IsAny<Identifier>()))
            .Returns(workflow);
        _workflowService.Setup(ws =>
                ws.CalculateShortestPathToEnd(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create(workflow.Journeys.Steps.Select(s => s.Value.ToStep().Value).Skip(2).ToList())
                .Value);
        var onboarding = OrganizationOnboardingRoot.Create(_recorder.Object, _identifierFactory.Object,
            _workflowService.Object, "anorganizationid".ToId()).Value;
        var navigatorRoles = Roles.Create(TenantRoles.Owner).Value;
        onboarding.MoveForward("anavigatorid".ToId(), navigatorRoles, "astepid1");
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboarding.ToOptional());
        await _application.MoveForwardAsync(_caller.Object, "anorganizationid", "astepid1", null,
            CancellationToken.None);
        await _application.MoveForwardAsync(_caller.Object, "anorganizationid", "astepid2", null,
            CancellationToken.None);
        _onboardingRepository.Invocations.Clear();

        var result = await _application.MoveBackwardAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.State!.CurrentStep.Id.Should().Be("astepid1");
        result.Value.State!.PathTaken.Should().OnlyContain(s => s.Id == "astartstepid");
        result.Value.State.PathAhead.Count.Should().Be(2);
        result.Value.State.PathAhead[0].Id.Should().Be("astepid2");
        result.Value.State.PathAhead[1].Id.Should().Be("anendstepid");
        _onboardingRepository.Verify(rep =>
            rep.SaveAsync(It.Is<OrganizationOnboardingRoot>(root =>
                root.OrganizationId == "anorganizationid".ToId()
                && root.State.Status == OnboardingStatus.InProgress
                && root.State.CurrentStepId == "astepid1"
                && root.State.AllValues.Items.Count == 0
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenUpdateCurrentStepAsyncAndOnboardingNotFound_ThenReturnsError()
    {
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OrganizationOnboardingRoot>.None);
        var stepValues = new Dictionary<string, string> { { "aname", "avalue" } };

        var result = await _application.UpdateCurrentStepAsync(_caller.Object, "anorganizationid", stepValues,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenUpdateCurrentStepAsync_ThenUpdatesCurrentStep()
    {
        var workflow = CreateSimpleTwoStepWorkflow();
        _workflowService.Setup(ws => ws.FindWorkflow(It.IsAny<Identifier>()))
            .Returns(workflow);
        var onboarding = OrganizationOnboardingRoot.Create(_recorder.Object, _identifierFactory.Object,
            _workflowService.Object, "anorganizationid".ToId()).Value;
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboarding.ToOptional());
        var stepValues = new Dictionary<string, string> { { "aname", "avalue" } };

        var result = await _application.UpdateCurrentStepAsync(_caller.Object, "anorganizationid", stepValues,
            CancellationToken.None);

        result.Should().BeSuccess();
        _onboardingRepository.Verify(rep =>
            rep.SaveAsync(It.Is<OrganizationOnboardingRoot>(root =>
                root.OrganizationId == "anorganizationid".ToId()
                && root.State.Status == OnboardingStatus.InProgress
                && root.State.CurrentStepId == "astartstepid"
                && root.State.AllValues.Items["aname"] == "avalue"
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenCompleteOnboardingAsyncAndOnboardingNotFound_ThenReturnsError()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, _emailDomainService.Object, OrganizationOwnership.Personal,
            "acreatorid".ToId(), EmailAddress.Create("auser@company.com").Value, UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        _organizationRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OrganizationOnboardingRoot>.None);

        var result =
            await _application.CompleteOnboardingAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenCompleteOnboardingAsync_ThenCompletesOnboarding()
    {
        var organization = OrganizationRoot.Create(_recorder.Object, _identifierFactory.Object,
            _tenantSettingService.Object, _emailDomainService.Object, OrganizationOwnership.Personal,
            "acreatorid".ToId(), EmailAddress.Create("auser@company.com").Value, UserClassification.Person,
            DisplayName.Create("aname").Value, DatacenterLocations.Local).Value;
        var workflow = CreateSimpleTwoStepWorkflow();
        _workflowService.Setup(ws => ws.FindWorkflow(It.IsAny<Identifier>()))
            .Returns(workflow);
        var onboarding = OrganizationOnboardingRoot.Create(_recorder.Object, _identifierFactory.Object,
            _workflowService.Object, "anorganizationid".ToId()).Value;
        var initiatorRoles = Roles.Create(TenantRoles.Owner).Value;
        organization.StartOnboarding(onboarding.Id, initiatorRoles);
        _organizationRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _organizationRepository.Setup(rep =>
                rep.SaveAsync(It.IsAny<OrganizationRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationRoot root, CancellationToken _) => root);
        _onboardingRepository.Setup(rep =>
                rep.FindByOrganizationIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboarding.ToOptional());
        _onboardingRepository.Invocations.Clear();

        var result =
            await _application.CompleteOnboardingAsync(_caller.Object, "anorganizationid", CancellationToken.None);

        result.Should().BeSuccess();
        _organizationRepository.Verify(rep =>
            rep.SaveAsync(It.Is<OrganizationRoot>(root =>
                root.OnboardingStatus == OnboardingStatus.Complete
            ), It.IsAny<CancellationToken>()));
        _onboardingRepository.Verify(rep =>
            rep.SaveAsync(It.Is<OrganizationOnboardingRoot>(root =>
                root.OrganizationId == "anorganizationid".ToId()
                && root.State.Status == OnboardingStatus.Complete
                && root.State.CurrentStepId == "astartstepid"
                && root.State.AllValues.Items.Count == 0
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    private WorkflowSchema CreateSimpleTwoStepWorkflow()
    {
        var start = StepSchema.Create("astartstepid", OnboardingStepType.Start, "astartstep", Optional<string>.None,
            "astepid1", [], 40, new Dictionary<string, string>()).Value;
        var step1 = StepSchema.Create("astepid1", OnboardingStepType.Normal, "astep1", Optional<string>.None,
            "astepid2", [], 30, new Dictionary<string, string>()).Value;
        var step2 = StepSchema.Create("astepid2", OnboardingStepType.Normal, "astep2", Optional<string>.None,
            "anendstepid", [], 30, new Dictionary<string, string>()).Value;
        var end = StepSchema.Create("anendstepid", OnboardingStepType.End, "anendstep", Optional<string>.None,
            Optional<string>.None, [], 0, new Dictionary<string, string>()).Value;
        var steps = new Dictionary<string, StepSchema>
        {
            { "astartstepid", start },
            { "astepid1", step1 },
            { "astepid2", step2 },
            { "anendstepid", end }
        };

        return WorkflowSchema.Create(_workflowService.Object, "aname", steps, "astartstepid", "anendstepid").Value;
    }

    private static OrganizationOnboardingWorkflowSchema CreateSimpleTwoStepWorkflowSchema()
    {
        return new OrganizationOnboardingWorkflowSchema
        {
            Name = "aname",
            StartStepId = "astartstepid",
            EndStepId = "anendstepid",
            Steps = new Dictionary<string, OrganizationOnboardingStepSchema>
            {
                {
                    "astartstepid", new OrganizationOnboardingStepSchema
                    {
                        Id = "astartstepid",
                        Type = OrganizationOnboardingStepSchemaType.Start,
                        Title = "Start Step",
                        NextStepId = "astepid1",
                        Weight = 40
                    }
                },
                {
                    "astepid1", new OrganizationOnboardingStepSchema
                    {
                        Id = "astepid1",
                        Type = OrganizationOnboardingStepSchemaType.Normal,
                        Title = "Step 1",
                        NextStepId = "astepid2",
                        Weight = 30
                    }
                },
                {
                    "astepid2", new OrganizationOnboardingStepSchema
                    {
                        Id = "astepid2",
                        Type = OrganizationOnboardingStepSchemaType.Normal,
                        Title = "Step 2",
                        NextStepId = "anendstepid",
                        Weight = 30
                    }
                },
                {
                    "anendstepid", new OrganizationOnboardingStepSchema
                    {
                        Id = "anendstepid",
                        Type = OrganizationOnboardingStepSchemaType.End,
                        Title = "End Step",
                        Weight = 0
                    }
                }
            }
        };
    }
}