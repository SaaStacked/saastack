using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations.Onboarding;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;
using FluentAssertions;
using Moq;
using OrganizationsDomain.DomainServices;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class OrganizationOnboardingRootSpec
{
    private readonly OrganizationOnboardingRoot _onboarding;
    private readonly WorkflowSchema _workflowSchema;

    public OrganizationOnboardingRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity _) => "anid".ToId());

        var workflowService = new Mock<IOnboardingWorkflowService>();
        workflowService.Setup(ws =>
                ws.ValidateWorkflow(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Result.Ok);
        _workflowSchema =
            Workflows.CreateSingleStepLinearWorkflow(workflowService.Object);
        workflowService.Setup(ws =>
                ws.CalculateShortestPath(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create(_workflowSchema.Journeys.Steps.Select(s => s.Value.ToStep().Value).ToList()).Value);
        workflowService.Setup(ws =>
                ws.CalculateShortestPathToEnd(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create([]).Value);
        workflowService.Setup(ws => ws.FindWorkflow(It.IsAny<Identifier>()))
            .Returns(_workflowSchema);

        _onboarding = OrganizationOnboardingRoot.Create(recorder.Object, identifierFactory.Object,
            workflowService.Object, "anorganizationid".ToId(), "aninitiatorid".ToId()).Value;
    }

    [Fact]
    public void WhenCreate_ThenOnboardingStarted()
    {
        _onboarding.OrganizationId.Should().Be("anorganizationid".ToId());
        _onboarding.InitiatedById.Should().Be("aninitiatorid".ToId());
        _onboarding.Workflow.Name.Should().Be(_workflowSchema.Name);
        _onboarding.State.Status.Should().Be(OnboardingStatus.InProgress);
        _onboarding.State.CurrentStepId.Should().Be("astartstepid");
        _onboarding.State.AllValues.Items.Should()
            .ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"));
        _onboarding.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenForceCompleteByMember_ThenReturnsError()
    {
        var result = _onboarding.ForceComplete(Roles.Empty, "acompleterid".ToId());

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationOnboardingRoot_UserNotOrgOwner);
    }

    [Fact]
    public void WhenForceCompleteAndNotStarted_ThenReturnsError()
    {
#if TESTINGONLY
        _onboarding.TestingOnly_SetStatus(OnboardingStatus.NotStarted);
#endif

        var result = _onboarding.ForceComplete(OwnerRoles, "acompleterid".ToId());

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OrganizationOnboardingRoot_NotStarted);
    }

    [Fact]
    public void WhenForceCompleteAndAlreadyComplete_ThenReturnsError()
    {
        _onboarding.ForceComplete(OwnerRoles, "acompleterid".ToId());

        var result = _onboarding.ForceComplete(OwnerRoles, "acompleterid".ToId());

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OrganizationOnboardingRoot_AlreadyCompleted);
    }

    [Fact]
    public void WhenForceCompleteAndNotAtEndNode_ThenCompletes()
    {
        _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "astepid1");
        _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "anendstepid");

        var result = _onboarding.ForceComplete(OwnerRoles, "acompleterid".ToId());

        result.Should().BeSuccess();
        _onboarding.State.Status.Should().Be(OnboardingStatus.Complete);
        _onboarding.State.CompletedBy.Should().Be("acompleterid");
        _onboarding.State.CompletedAt.Should().NotBeNull();
        _onboarding.Events.Last().Should().BeOfType<Completed>();
    }

    [Fact]
    public void WhenMoveBackwardByMember_ThenReturnsError()
    {
        var result = _onboarding.MoveBackward("anavigatorid".ToId(), Roles.Empty);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationOnboardingRoot_UserNotOrgOwner);
    }

    [Fact]
    public void WhenMoveBackwardAndNotStarted_ThenReturnsError()
    {
#if TESTINGONLY
        _onboarding.TestingOnly_SetStatus(OnboardingStatus.NotStarted);
#endif

        var result = _onboarding.MoveBackward("anavigatorid".ToId(), OwnerRoles);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OrganizationOnboardingRoot_NotStarted);
    }

    [Fact]
    public void WhenMoveBackwardAndAlreadyComplete_ThenReturnsError()
    {
        _onboarding.ForceComplete(OwnerRoles, "acompleterid".ToId());

        var result = _onboarding.MoveBackward("anavigatorid".ToId(), OwnerRoles);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OrganizationOnboardingRoot_AlreadyCompleted);
    }

    [Fact]
    public void WhenMoveBackwardAndAtStartStep_ThenReturnsError()
    {
        var result = _onboarding.MoveBackward("anavigatorid".ToId(), OwnerRoles);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OrganizationOnboardingRoot_MoveBackward_AtStartStep);
    }

    [Fact]
    public void WhenMoveBackwardAndNotAtFirstStep_ThenMovesBackward()
    {
        _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "astepid1");

        var result = _onboarding.MoveBackward("anavigatorid".ToId(), OwnerRoles);

        result.Should().BeSuccess();
        _onboarding.State.AllValues.Items.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"));
        _onboarding.Events.Last().Should().BeOfType<StepNavigated>();
    }

    [Fact]
    public void WhenMoveForwardByMember_ThenReturnsError()
    {
        var result = _onboarding.MoveForward("anavigatorid".ToId(), Roles.Empty, "astepid1");

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationOnboardingRoot_UserNotOrgOwner);
    }

    [Fact]
    public void WhenMoveForwardAndNotStarted_ThenReturnsError()
    {
#if TESTINGONLY
        _onboarding.TestingOnly_SetStatus(OnboardingStatus.NotStarted);
#endif

        var result = _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "astepid1");

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OrganizationOnboardingRoot_NotStarted);
    }

    [Fact]
    public void WhenMoveForwardAndAlreadyComplete_ThenReturnsError()
    {
        _onboarding.ForceComplete(OwnerRoles, "acompleterid".ToId());

        var result = _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "astepid1");

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OrganizationOnboardingRoot_AlreadyCompleted);
    }

    [Fact]
    public void WhenMoveForwardAndAtEndStep_ThenReturnsError()
    {
        _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "astepid1");
        _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "anendstepid");

        var result = _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "astepid1");

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OrganizationOnboardingRoot_MoveForward_AtEndStep);
    }

    [Fact]
    public void WhenMoveForwardAndUnknownStep_ThenReturnsError()
    {
        var result = _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "unknownstep");

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.WorkflowSchema_ValidateMove_UnknownToStep.Format("unknownstep"));
    }

    [Fact]
    public void WhenMoveForwardAndUnreachableStep_ThenReturnsError()
    {
        _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, Optional<string>.None);

        var result = _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "astartstepid");

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.WorkflowSchema_ValidateMove_NotDirectlyReachable.Format("astartstepid", "astepid1"));
    }

    [Fact]
    public void WhenMoveForwardToValidForwardStep_ThenMovesForward()
    {
        var result = _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, "astepid1");

        result.Should().BeSuccess();
        result.Value.Should().Be("astepid1");
        _onboarding.State.AllValues.Items.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"));
        _onboarding.Events.Last().Should().BeOfType<StepNavigated>();
    }

    [Fact]
    public void WhenMoveForwardWithNoStepId_ThenMovesForward()
    {
        var result = _onboarding.MoveForward("anavigatorid".ToId(), OwnerRoles, Optional<string>.None);

        result.Should().BeSuccess();
        result.Value.Should().Be("astepid1");
        _onboarding.State.AllValues.Items.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"));
        _onboarding.Events.Last().Should().BeOfType<StepNavigated>();
    }

    [Fact]
    public void WhenUpdateCurrentStepByMember_ThenReturnsError()
    {
        var result = _onboarding.UpdateCurrentStep(Roles.Empty, StringNameValues.Empty);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.OrganizationOnboardingRoot_UserNotOrgOwner);
    }

    [Fact]
    public void WhenUpdateCurrentStepAndNotStarted_ThenReturnsError()
    {
#if TESTINGONLY
        _onboarding.TestingOnly_SetStatus(OnboardingStatus.NotStarted);
#endif

        var result = _onboarding.UpdateCurrentStep(OwnerRoles, StringNameValues.Empty);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OrganizationOnboardingRoot_NotStarted);
    }

    [Fact]
    public void WhenUpdateCurrentStepAndAlreadyComplete_ThenReturnsError()
    {
        _onboarding.ForceComplete(OwnerRoles, "acompleterid".ToId());

        var result = _onboarding.UpdateCurrentStep(OwnerRoles, StringNameValues.Empty);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OrganizationOnboardingRoot_AlreadyCompleted);
    }

    [Fact]
    public void WhenUpdateCurrentStepAndInProgress_ThenUpdatesState()
    {
        var values = StringNameValues.Create(new Dictionary<string, string>
        {
            { "aname1", "anothervalue" },
            { "aname2", "avalue2" },
            { "aname3", "avalue3" }
        }).Value;

        var result = _onboarding.UpdateCurrentStep(OwnerRoles, values);

        result.Should().BeSuccess();
        _onboarding.State.CurrentStepId.Should().Be("astartstepid");
        _onboarding.State.AllValues.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "anothervalue"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3"));
        _onboarding.State.AllValues.Items.Should().BeEquivalentTo(values.Items);
        _onboarding.Events.Last().Should().BeOfType<StepStateChanged>();
    }

    private static Roles OwnerRoles => Roles.Create(TenantRoles.Owner).Value;
}