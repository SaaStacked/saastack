using Common;
using Common.Extensions;
using Domain.Shared;
using FluentAssertions;
using Moq;
using OrganizationsDomain.DomainServices;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class WorkflowSchemaSpec
{
    private readonly Mock<IOnboardingWorkflowService> _workflowService;

    public WorkflowSchemaSpec()
    {
        _workflowService = new Mock<IOnboardingWorkflowService>();
        _workflowService.Setup(ws =>
                ws.CalculateShortestPath(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Empty);
    }

    [Fact]
    public void WhenEmpty_ThenReturnsEmpty()
    {
        var result = WorkflowSchema.Empty;

        result.Name.Should().Be("empty");
        result.Journeys.Should().Be(JourneySchema.Empty);
        result.StartStepId.Should().BeEmpty();
        result.EndStepId.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithEmptyName_ThenReturnsError()
    {
        var start = Workflows.CreateStartStep("astartstepid", "astepid", 100);
        var end = Workflows.CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema> { { "astartstepid", start }, { "anendstepid", end } };

        var result = WorkflowSchema.Create(_workflowService.Object, string.Empty, steps, "astartstepid", "anendstepid");

        result.Should().BeError(ErrorCode.Validation, Resources.OnboardingWorkflow_InvalidName);
    }

    [Fact]
    public void WhenCreateWithEmptyStartStepId_ThenReturnsError()
    {
        var start = Workflows.CreateStartStep("astartstepid", "astepid", 100);
        var end = Workflows.CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema> { { "astartstepid", start }, { "anendstepid", end } };

        var result = WorkflowSchema.Create(_workflowService.Object, "aname", steps, string.Empty, "anendstepid");

        result.Should().BeError(ErrorCode.Validation, Resources.OnboardingWorkflow_InvalidStartStepId);
    }

    [Fact]
    public void WhenCreateWithEmptyEndStepId_ThenReturnsError()
    {
        var start = Workflows.CreateStartStep("astartstepid", "astepid", 100);
        var end = Workflows.CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema> { { "astartstepid", start }, { "anendstepid", end } };

        var result = WorkflowSchema.Create(_workflowService.Object, "aname", steps, "astartstepid", string.Empty);

        result.Should().BeError(ErrorCode.Validation, Resources.OnboardingWorkflow_InvalidEndStepId);
    }

    [Fact]
    public void WhenCreateWithNoSteps_ThenReturnsError()
    {
        var result = WorkflowSchema.Create(_workflowService.Object, "aname", new Dictionary<string, StepSchema>(),
            "astartstepid",
            "anendstepid");

        result.Should().BeError(ErrorCode.Validation, Resources.OnboardingWorkflow_NoSteps);
    }

    [Fact]
    public void WhenCreateAndStartStepNotInSteps_ThenReturnsError()
    {
        var start = Workflows.CreateStartStep("astartstepid", "anendstepid", 100);
        var end = Workflows.CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema> { { "astartstepid", start }, { "anendstepid", end } };

        var result = WorkflowSchema.Create(_workflowService.Object, "aname", steps, "aunknownstepid", "anendstepid");

        result.Should().BeError(ErrorCode.Validation,
            Resources.WorkflowSchema_StartStepMissingFromSteps.Format("aunknownstepid"));
    }

    [Fact]
    public void WhenCreateAndEndStepNonExistentInSteps_ThenReturnsError()
    {
        var start = Workflows.CreateStartStep("astartstepid", "anendstepid", 100);
        var end = Workflows.CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema> { { "astartstepid", start }, { "anendstepid", end } };

        var result = WorkflowSchema.Create(_workflowService.Object, "aname", steps, "astartstepid", "aunknownstepid");

        result.Should().BeError(ErrorCode.Validation,
            Resources.WorkflowSchema_EndStepMissingFromSteps.Format("aunknownstepid"));
    }

    [Fact]
    public void WhenCreateAndStartStepNotOfTypeStart_ThenReturnsError()
    {
        var start = Workflows.CreateRegularStep("astartstepid", "anendstepid", 100);
        var end = Workflows.CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema> { { "astartstepid", start }, { "anendstepid", end } };

        var result = WorkflowSchema.Create(_workflowService.Object, "aname", steps, "astartstepid", "anendstepid");

        result.Should().BeError(ErrorCode.Validation,
            Resources.WorkflowSchema_StartNodeIncorrectType.Format(OnboardingStepType.Start));
    }

    [Fact]
    public void WhenCreateAndEndStepNotOfTypeEnd_ThenReturnsError()
    {
        var start = Workflows.CreateStartStep("astartstepid", "anendstepid", 100);
        var end = Workflows.CreateRegularStep("anendstepid", "nowhere", 0);
        var steps = new Dictionary<string, StepSchema> { { "astartstepid", start }, { "anendstepid", end } };

        var result = WorkflowSchema.Create(_workflowService.Object, "aname", steps, "astartstepid", "anendstepid");

        result.Should().BeError(ErrorCode.Validation,
            Resources.WorkflowSchema_EndNodeIncorrectType.Format(OnboardingStepType.End));
    }

    [Fact]
    public void WhenCreateWithOneStep_ThenReturnsWorkflowSchema()
    {
        var start = Workflows.CreateStartStep("astartstepid", "astepid", 50);
        var step1 = Workflows.CreateRegularStep("astepid", "anendstepid", 50);
        var end = Workflows.CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema>
        {
            { "astartstepid", start },
            { "astepid", step1 },
            { "anendstepid", end }
        };

        var result = WorkflowSchema.Create(_workflowService.Object, "aname", steps, "astartstepid", "anendstepid");

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
        result.Value.StartStepId.Should().Be("astartstepid");
        result.Value.EndStepId.Should().Be("anendstepid");
        result.Value.Journeys.Steps.Should().HaveCount(3);
    }

    [Fact]
    public void WhenDetermineNextStepWithNonExistentCurrentStep_ThenReturnsError()
    {
        var workflow = Workflows.CreateTwoStepLinearWorkflow(_workflowService.Object);

        var result = workflow.DetermineNextStep("anunknownstepid", StringNameValues.Empty);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.WorkflowSchema_DetermineNextStep_UnknownCurrentStep.Format("anunknownstepid"));
    }

    [Fact]
    public void WhenDetermineNextStepFromEndStep_ThenReturnsError()
    {
        var workflow = Workflows.CreateTwoStepLinearWorkflow(_workflowService.Object);

        var result = workflow.DetermineNextStep("anendstepid", StringNameValues.Empty);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.WorkflowSchema_DetermineNextStep_EndStep);
    }

    [Fact]
    public void WhenDetermineNextStepFromBranchWithNoMatchingCondition_ThenReturnsFirstBranchStep()
    {
        var workflow = Workflows.CreateMultiBranchWorkflow(_workflowService.Object);

        var values = StringNameValues.Create(new Dictionary<string, string> { { "afield", "anothervalue" } }).Value;

        var result = workflow.DetermineNextStep("abranchstepid", values);

        result.Should().BeSuccess();
        result.Value.Should().Be("astepid1");
    }

    [Fact]
    public void WhenDetermineNextStepFromBranchWithMatchingCondition_ThenReturnsBranchStep()
    {
        var workflow = Workflows.CreateMultiBranchWorkflow(_workflowService.Object);

        var values = StringNameValues.Create(new Dictionary<string, string> { { "afield", "avalue" } }).Value;

        var result = workflow.DetermineNextStep("abranchstepid", values);

        result.Should().BeSuccess();
        result.Value.Should().Be("astepid1");
    }

    [Fact]
    public void WhenDetermineNextStepFromNormalStep_ThenReturnsNextStep()
    {
        var workflow = Workflows.CreateTwoStepLinearWorkflow(_workflowService.Object);

        var result = workflow.DetermineNextStep("astepid1", StringNameValues.Empty);

        result.Should().BeSuccess();
        result.Value.Should().Be("astepid2");
    }

    [Fact]
    public void WhenInitiateStart_ThenReturnsInProgressStartingStep()
    {
        var start = Workflows.CreateStartStep("astartstepid", "astepid", 50,
            new Dictionary<string, string> { { "aname1", "avalue1" } });
        var step1 = Workflows.CreateRegularStep("astepid", "anendstepid", 50,
            new Dictionary<string, string> { { "aname2", "avalue2" } });
        var end = Workflows.CreateEndStep("anendstepid", new Dictionary<string, string> { { "aname3", "avalue3" } });
        var steps = new Dictionary<string, StepSchema>
        {
            { "astartstepid", start },
            { "astepid", step1 },
            { "anendstepid", end }
        };
        var workflow = WorkflowSchema.Create(_workflowService.Object, "aname", steps, "astartstepid", "anendstepid")
            .Value;
        _workflowService.Setup(ws =>
                ws.CalculateShortestPath(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create([start.ToStep().Value, step1.ToStep().Value, end.ToStep().Value]).Value);

        var result = workflow.InitiateStart(_workflowService.Object);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.InProgress);
        result.Value.CurrentStepId.Should().Be("astartstepid");
        result.Value.PathTaken.Should().Be(Journey.Empty);
        result.Value.PathAhead.Steps.Should().HaveCount(2);
        result.Value.PathAhead.Steps[0].StepId.Should().Be("astepid");
        result.Value.PathAhead.Steps[1].StepId.Should().Be("anendstepid");
        result.Value.TotalWeight.Should().Be(100);
        result.Value.CompletedWeight.Should().Be(0);
        result.Value.AllValues.Items.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"));
        result.Value.StartedAt.Should().NotBeNone();
        result.Value.CompletedAt.Should().BeNone();
        result.Value.CompletedBy.Should().BeNone();
    }

    [Fact]
    public void WhenTryGetStepWithExistingStep_ThenReturnsTrue()
    {
        var start = Workflows.CreateStartStep("astartstepid", "anendstepid", 100);
        var end = Workflows.CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema> { { "astartstepid", start }, { "anendstepid", end } };
        var workflow = WorkflowSchema.Create(_workflowService.Object, "aname", steps, "astartstepid", "anendstepid")
            .Value;

        var result = workflow.TryGetStep("astartstepid", out var step);

        result.Should().BeTrue();
        step.Should().NotBeNull();
        step!.Id.Should().Be("astartstepid");
    }

    [Fact]
    public void WhenTryGetStepWithNonExistingStep_ThenReturnsFalse()
    {
        var start = Workflows.CreateStartStep("astartstepid", "anendstepid", 100);
        var end = Workflows.CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema> { { "astartstepid", start }, { "anendstepid", end } };
        var workflow = WorkflowSchema.Create(_workflowService.Object, "aname", steps, "astartstepid", "anendstepid")
            .Value;

        var result = workflow.TryGetStep("anunknownstepid", out var step);

        result.Should().BeFalse();
        step.Should().BeNull();
    }

    [Fact]
    public void WhenCreateWithSimpleLinearWorkflow_ThenCreates()
    {
        var result = Workflows.CreateTwoStepLinearWorkflow(_workflowService.Object);

        result.Should().NotBeNull();
        result.Name.Should().Be("aname");
    }

    [Fact]
    public void WhenCreateWithMultiBranchWorkflow_ThenCreates()
    {
        var result = Workflows.CreateMultiBranchWorkflow(_workflowService.Object);

        result.Should().NotBeNull();
        result.Name.Should().Be("aname");
    }

    [Fact]
    public void WhenCreateWithMultiBranchWorkflow2_ThenCreates()
    {
        var result = Workflows.CreateMultiBranchWorkflow2(_workflowService.Object);

        result.Should().NotBeNull();
        result.Name.Should().Be("aname");
    }
}