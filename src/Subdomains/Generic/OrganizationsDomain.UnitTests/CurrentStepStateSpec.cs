using Common;
using Common.Extensions;
using Domain.Shared;
using FluentAssertions;
using Moq;
using OrganizationsDomain.DomainServices;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class CurrentStepStateSpec
{
    private readonly Mock<IOnboardingWorkflowService> _workflowService;

    public CurrentStepStateSpec()
    {
        _workflowService = new Mock<IOnboardingWorkflowService>();
        _workflowService.Setup(ws =>
                ws.CalculateShortestPathToEnd(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Empty);
    }

    [Fact]
    public void WhenEmpty_ThenReturnsEmpty()
    {
        var result = CurrentStepState.Empty;

        result.Status.Should().Be(OnboardingStatus.NotStarted);
        result.CurrentStepId.Should().BeEmpty();
        result.PathTaken.Should().Be(Journey.Empty);
        result.PathAhead.Should().Be(Journey.Empty);
        result.TotalWeight.Should().Be(0);
        result.CompletedWeight.Should().Be(0);
        result.ProgressPercentage.Should().Be(0);
        result.AllValues.Items.Should().BeEmpty();
        result.StartedAt.Should().BeNone();
        result.CompletedAt.Should().BeNone();
        result.EnteredAt.Should().BeNear(DateTime.UtcNow);
        result.CompletedBy.Should().BeNone();
    }

    [Fact]
    public void WhenCreateWithEmptyCurrentStepId_ThenReturnsError()
    {
        var result = CurrentStepState.Create(
            OnboardingStatus.NotStarted,
            string.Empty,
            Journey.Empty,
            Journey.Empty,
            0,
            0,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None);

        result.Should().BeError(ErrorCode.Validation, Resources.OnboardingState_InvalidCurrentStepId);
    }

    [Fact]
    public void WhenCreateWithNegativeTotalWeight_ThenReturnsError()
    {
        var result = CurrentStepState.Create(
            OnboardingStatus.NotStarted,
            "astepid",
            Journey.Empty,
            Journey.Empty,
            -1,
            0,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None);

        result.Should().BeError(ErrorCode.Validation, Resources.OnboardingState_InvalidTotalWeight);
    }

    [Fact]
    public void WhenCreateWithNegativeCompletedWeight_ThenReturnsError()
    {
        var result = CurrentStepState.Create(
            OnboardingStatus.NotStarted,
            "astepid",
            Journey.Empty,
            Journey.Empty,
            100,
            -1,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None);

        result.Should().BeError(ErrorCode.Validation, Resources.OnboardingState_InvalidCompletedWeight.Format(-1));
    }

    [Fact]
    public void WhenCreateWithCompletedWeightGreaterThanTotalWeight_ThenReturnsError()
    {
        var result = CurrentStepState.Create(
            OnboardingStatus.NotStarted,
            "astepid",
            Journey.Empty,
            Journey.Empty,
            100,
            150,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None);

        result.Should().BeError(ErrorCode.Validation, Resources.OnboardingState_InvalidCompletedWeight.Format(150));
    }

    [Fact]
    public void WhenCreateWithCompleteStatusAndNoCompletedDate_ThenReturnsError()
    {
        var result = CurrentStepState.Create(
            OnboardingStatus.Complete,
            "astepid",
            Journey.Empty,
            Journey.Empty,
            100,
            100,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None);

        result.Should().BeError(ErrorCode.Validation, Resources.OnboardingState_RequiresCompletedDate);
    }

    [Fact]
    public void WhenCreates_ThenReturnsInitialOnboardingState()
    {
        var step1 = Step.Create("astepid1", "atitle1", 10, DateTime.UtcNow, Optional<DateTime>.None,
            StringNameValues.Empty).Value;
        var step2 = Step.Create("astepid2", "atitle2", 10, DateTime.UtcNow, Optional<DateTime>.None,
            StringNameValues.Empty).Value;
        var values = StringNameValues.Create(new Dictionary<string, string> { { "aname1", "value1" } }).Value;

        var result = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "currentstep",
            Journey.Create([step1]).Value,
            Journey.Create([step2]).Value,
            100,
            50,
            values,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.InProgress);
        result.Value.CurrentStepId.Should().Be("currentstep");
        result.Value.PathTaken.Steps.Should().ContainSingle().Which.Should().Be(step1);
        result.Value.PathAhead.Steps.Should().ContainSingle().Which.Should().Be(step2);
        result.Value.TotalWeight.Should().Be(100);
        result.Value.CompletedWeight.Should().Be(50);
        result.Value.ProgressPercentage.Should().Be(50);
        result.Value.AllValues.Items.Should()
            .ContainSingle(kvp => kvp.Key == "aname1" && kvp.Value == "value1");
        result.Value.StartedAt.Should().NotBeNone();
        result.Value.CompletedAt.Should().BeNone();
        result.Value.EnteredAt.Should().BeNear(DateTime.UtcNow);
        result.Value.CompletedBy.Should().BeNone();
    }

    [Fact]
    public void WhenCreateWithCompleteStatusAndCompletedDate_ThenReturnsOnboardingState()
    {
        var completedAt = DateTime.UtcNow;

        var result = CurrentStepState.Create(
            OnboardingStatus.Complete,
            "astepid",
            Journey.Empty,
            Journey.Empty,
            100,
            100,
            StringNameValues.Empty,
            DateTime.UtcNow,
            completedAt.ToOptional(),
            "auserid".ToOptional());

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.Complete);
        result.Value.CompletedAt.Should().BeSome(completedAt);
        result.Value.CompletedBy.Should().BeSome("auserid");
    }

    [Fact]
    public void WhenMarkCompleteOnStepOfJourney_ThenMarksComplete()
    {
        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid",
            Journey.Empty,
            Journey.Empty,
            100,
            50,
            StringNameValues.Empty,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result = state.MarkComplete("auserid");

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.Complete);
        result.Value.CurrentStepId.Should().Be("astepid");
        result.Value.PathTaken.Steps.Should().BeEmpty();
        result.Value.PathAhead.Steps.Should().BeEmpty();
        result.Value.TotalWeight.Should().Be(100);
        result.Value.CompletedWeight.Should().Be(50);
        result.Value.AllValues.Items.Should().BeEmpty();
        result.Value.StartedAt.Should().BeNear(DateTime.UtcNow);
        result.Value.CompletedAt.Should().NotBeNone();
        result.Value.CompletedBy.Should().BeSome("auserid");
        result.Value.EnteredAt.Should().BeNear(DateTime.UtcNow);
        result.Value.ProgressPercentage.Should().Be(50);
    }

    [Fact]
    public void WhenNavigateToStepAndIsAlreadyCompleted_ThenReturnsError()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var state = CurrentStepState.Create(
            OnboardingStatus.Complete,
            "astepid1",
            Journey.Empty,
            Journey.Empty,
            100,
            0,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            DateTime.UtcNow,
            "acompleterid").Value;

        var result = state.NavigateToStep(_workflowService.Object, "anunknownstepid", "astepid2", workflow);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.CurrentStepState_NavigateToStep_AlreadyCompleted);
    }

    [Fact]
    public void WhenNavigateToStepAndFromIsNotCurrentStep_ThenReturnsError()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var state = CurrentStepState.Create(
            OnboardingStatus.NotStarted,
            "astepid1",
            Journey.Empty,
            Journey.Empty,
            100,
            0,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result = state.NavigateToStep(_workflowService.Object, "anunknownstepid", "astepid2", workflow);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.CurrentStepState_NavigateToStep_NotFromCurrentStep.Format("anunknownstepid", "astepid1"));
    }

    [Fact]
    public void WhenNavigateToStepAndFromIsUnknown_ThenReturnsError()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var state = CurrentStepState.Create(
            OnboardingStatus.NotStarted,
            "anunknownstepid",
            Journey.Empty,
            Journey.Empty,
            100,
            0,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result = state.NavigateToStep(_workflowService.Object, "anunknownstepid", "astepid2", workflow);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.CurrentStepState_NavigateToStep_UnknownCurrentStep.Format("anunknownstepid", "astepid1"));
    }

    [Fact]
    public void WhenNavigateToStepAndCurrentStepIsEndStep_ThenReturnsError()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var state = CurrentStepState.Create(
            OnboardingStatus.NotStarted,
            "anendstepid",
            Journey.Empty,
            Journey.Empty,
            100,
            0,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result = state.NavigateToStep(_workflowService.Object, "anendstepid", "astepid2", workflow);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.CurrentStepState_NavigateToStep_ForwardFromEnd.Format("anendstepid"));
    }

    [Fact]
    public void WhenNavigateToStepFromNotStarted_ThenChangesStatusToInProgress()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var state = CurrentStepState.Create(
            OnboardingStatus.NotStarted,
            "astepid1",
            Journey.Empty,
            Journey.Empty,
            100,
            0,
            StringNameValues.Empty,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result = state.NavigateToStep(_workflowService.Object, "astepid1", "astepid2", workflow);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.InProgress);
    }

    [Fact]
    public void WhenNavigateFromAnyStepOnJourneyForwardToAnyStepOnJourney_ThenNavigates()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid1",
            Journey.Empty,
            Journey.Empty,
            100,
            0,
            StringNameValues.Create(new Dictionary<string, string>
            {
                { "aname1", "avalue1" },
                { "aname2", "avalue2" }
            }).Value,
            Optional<DateTime>.None,
            Optional<DateTime>.None,
            Optional<string>.None).Value;
        var end = Step.Create("anendstepid", "atitle", 0, Optional<DateTime>.None, Optional<DateTime>.None,
            StringNameValues.Create(new Dictionary<string, string>
            {
                { "aname1", "avalue1" },
                { "aname2", "avalue2" }
            }).Value).Value;
        _workflowService.Setup(ws =>
                ws.CalculateShortestPathToEnd(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create([end]).Value);

        var result = state.NavigateToStep(_workflowService.Object, "astepid1", "astepid2", workflow);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.InProgress);
        result.Value.CurrentStepId.Should().Be("astepid2");
        result.Value.PathTaken.Steps.Should().ContainSingle().Which.StepId.Should().Be("astepid1");
        result.Value.PathTaken.Steps[0].Values.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"));
        result.Value.PathAhead.Steps.Count.Should().Be(1);
        result.Value.PathAhead.Steps[0].StepId.Should().Be("anendstepid");
        result.Value.TotalWeight.Should().Be(100);
        result.Value.CompletedWeight.Should().Be(30);
        result.Value.AllValues.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3"));
        result.Value.StartedAt.Should().BeNear(DateTime.UtcNow);
        result.Value.CompletedAt.Should().BeNone();
        result.Value.CompletedBy.Should().BeNone();
        result.Value.EnteredAt.Should().BeNear(DateTime.UtcNow);
        result.Value.ProgressPercentage.Should().Be(30);
    }

    [Fact]
    public void WhenNavigateToStepFromMidJourneyForwardToNextStep_ThenNavigates()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var start = workflow.CreateStateStep("astartstepid").Value;
        var step2 = workflow.CreateStateStep("astepid2").Value;
        var end = workflow.CreateStateStep("anendstepid").Value;

        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid1",
            Journey.Create([start]).Value,
            Journey.Create([step2, end]).Value,
            100,
            40,
            StringNameValues.Create(new Dictionary<string, string>
            {
                { "aname1", "avalue1" },
                { "aname2", "avalue2" }
            }).Value,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;
        _workflowService.Setup(ws =>
                ws.CalculateShortestPathToEnd(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create([end]).Value);

        var result = state.NavigateToStep(_workflowService.Object, "astepid1", "astepid2", workflow);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.InProgress);
        result.Value.CurrentStepId.Should().Be("astepid2");
        result.Value.PathTaken.Steps.Should().HaveCount(2);
        result.Value.PathTaken.Steps[0].StepId.Should().Be("astartstepid");
        result.Value.PathTaken.Steps[1].StepId.Should().Be("astepid1");
        result.Value.PathAhead.Steps.Should().ContainSingle().Which.StepId.Should().Be("anendstepid");
        result.Value.TotalWeight.Should().Be(100);
        result.Value.CompletedWeight.Should().Be(70);
        result.Value.AllValues.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3"));
        result.Value.StartedAt.Should().BeNear(DateTime.UtcNow);
        result.Value.CompletedAt.Should().BeNone();
        result.Value.CompletedBy.Should().BeNone();
        result.Value.EnteredAt.Should().BeNear(DateTime.UtcNow);
        result.Value.ProgressPercentage.Should().Be(70);
    }

    [Fact]
    public void WhenNavigateToStepFromAnyStepForwardToEndStep_ThenNavigates()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var start = workflow.CreateStateStep("astartstepid").Value;
        var step1 = workflow.CreateStateStep("astepid1").Value;
        var step2 = workflow.CreateStateStep("astepid2").Value;
        var end = workflow.CreateStateStep("anendstepid").Value;
        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid2",
            Journey.Create([start, step1]).Value,
            Journey.Create([end]).Value,
            100,
            start.Weight + step1.Weight,
            StringNameValues.Create(new Dictionary<string, string>
            {
                { "aname1", "avalue1" },
                { "aname2", "avalue2" },
                { "aname3", "avalue3" }
            }).Value,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result = state.NavigateToStep(_workflowService.Object, "astepid2", "anendstepid", workflow);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.InProgress);
        result.Value.CurrentStepId.Should().Be("anendstepid");
        result.Value.PathTaken.Steps.Should().HaveCount(3);
        result.Value.PathTaken.Steps[0].StepId.Should().Be("astartstepid");
        result.Value.PathTaken.Steps[1].StepId.Should().Be("astepid1");
        result.Value.PathTaken.Steps[2].StepId.Should().Be("astepid2");
        result.Value.PathAhead.Steps.Should().BeEmpty();
        result.Value.TotalWeight.Should().Be(100);
        result.Value.CompletedWeight.Should().Be(start.Weight + step1.Weight + step2.Weight);
        result.Value.AllValues.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3"),
            new KeyValuePair<string, string>("aname4", "avalue4"));
        result.Value.StartedAt.Should().BeNear(DateTime.UtcNow);
        result.Value.CompletedAt.Should().BeNone();
        result.Value.CompletedBy.Should().BeNone();
        result.Value.EnteredAt.Should().BeNear(DateTime.UtcNow);
        result.Value.ProgressPercentage.Should().Be(100);
    }

    [Fact]
    public void WhenNavigateToStepFromAnyStepForwardFromEndStep_ThenReturnsError()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var start = workflow.CreateStateStep("astartstepid").Value;
        var step1 = workflow.CreateStateStep("astepid1").Value;
        var step2 = workflow.CreateStateStep("astepid2").Value;
        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "anendstepid",
            Journey.Create([start, step1, step2]).Value,
            Journey.Empty,
            100,
            start.Weight + step1.Weight + step2.Weight,
            StringNameValues.Empty,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result = state.NavigateToStep(_workflowService.Object, "anendstepid", "anendstepid", workflow);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.CurrentStepState_NavigateToStep_ForwardFromEnd.Format("anendstepid"));
    }

    [Fact]
    public void WhenNavigateToStepFromMidJourneyBackwardAStep_ThenNavigates()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var start = workflow.CreateStateStep("astartstepid").Value;
        var step1 = workflow.CreateStateStep("astepid1").Value;
        var step2 = workflow.CreateStateStep("astepid2").Value;
        var end = workflow.CreateStateStep("anendstepid").Value;

        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid2",
            Journey.Create([start, step1]).Value,
            Journey.Create([end]).Value,
            100,
            start.Weight + step1.Weight,
            StringNameValues.Create(new Dictionary<string, string>
            {
                { "aname1", "avalue1" },
                { "aname2", "avalue2" },
                { "aname3", "avalue3" }
            }).Value,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;
        _workflowService.Setup(ws =>
                ws.CalculateShortestPathToEnd(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create([step2, end]).Value);

        var result = state.NavigateToStep(_workflowService.Object, "astepid2", "astepid1", workflow);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.InProgress);
        result.Value.CurrentStepId.Should().Be("astepid1");
        result.Value.PathTaken.Steps.Should().HaveCount(1);
        result.Value.PathTaken.Steps[0].StepId.Should().Be("astartstepid");
        result.Value.PathAhead.Steps.Count.Should().Be(2);
        result.Value.PathAhead.Steps[0].StepId.Should().Be("astepid2");
        result.Value.PathAhead.Steps[1].StepId.Should().Be("anendstepid");
        result.Value.TotalWeight.Should().Be(100);
        result.Value.CompletedWeight.Should().Be(start.Weight);
        result.Value.AllValues.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3")
        );
        result.Value.StartedAt.Should().BeNear(DateTime.UtcNow);
        result.Value.CompletedAt.Should().BeNone();
        result.Value.CompletedBy.Should().BeNone();
        result.Value.EnteredAt.Should().BeNear(DateTime.UtcNow);
        result.Value.ProgressPercentage.Should().Be(40);
    }

    [Fact]
    public void WhenNavigateToStepFromMidJourneyBackwardToStartStep_ThenNavigates()
    {
        var workflow = CreateTwoStepWorkflow(_workflowService.Object);
        var start = workflow.CreateStateStep("astartstepid").Value;
        var step1 = workflow.CreateStateStep("astepid1").Value;
        var step2 = workflow.CreateStateStep("astepid2").Value;
        var end = workflow.CreateStateStep("anendstepid").Value;

        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid1",
            Journey.Create([start]).Value,
            Journey.Create([step2, end]).Value,
            100,
            start.Weight,
            StringNameValues.Create(new Dictionary<string, string>
            {
                { "aname1", "avalue1" },
                { "aname2", "avalue2" }
            }).Value,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;
        _workflowService.Setup(ws =>
                ws.CalculateShortestPathToEnd(It.IsAny<Dictionary<string, StepSchema>>(), It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Journey.Create([step1, step2, end]).Value);

        var result = state.NavigateToStep(_workflowService.Object, "astepid1", "astartstepid", workflow);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.InProgress);
        result.Value.CurrentStepId.Should().Be("astartstepid");
        result.Value.PathTaken.Steps.Should().HaveCount(0);
        result.Value.PathAhead.Steps.Count.Should().Be(3);
        result.Value.PathAhead.Steps[0].StepId.Should().Be("astepid1");
        result.Value.PathAhead.Steps[1].StepId.Should().Be("astepid2");
        result.Value.PathAhead.Steps[2].StepId.Should().Be("anendstepid");
        result.Value.TotalWeight.Should().Be(100);
        result.Value.CompletedWeight.Should().Be(0);
        result.Value.AllValues.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"));
        result.Value.StartedAt.Should().BeNear(DateTime.UtcNow);
        result.Value.CompletedAt.Should().BeNone();
        result.Value.CompletedBy.Should().BeNone();
        result.Value.EnteredAt.Should().BeNear(DateTime.UtcNow);
        result.Value.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void WhenUpdateStepValuesWithNewValues_ThenAddsValues()
    {
        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid1",
            Journey.Empty,
            Journey.Empty,
            100,
            0,
            StringNameValues.Create(new Dictionary<string, string>
            {
                { "aname1", "avalue1" },
                { "aname2", "avalue2" }
            }).Value,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result =
            state.UpdateCurrentStepValues(StringNameValues
                .Create(new Dictionary<string, string>
                {
                    { "afield1", "afieldvalue1" },
                    { "afield2", "afieldvalue2" }
                })
                .Value);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(OnboardingStatus.InProgress);
        result.Value.CurrentStepId.Should().Be("astepid1");
        result.Value.PathTaken.Steps.Should().BeEmpty();
        result.Value.PathAhead.Steps.Should().BeEmpty();
        result.Value.TotalWeight.Should().Be(100);
        result.Value.CompletedWeight.Should().Be(0);
        result.Value.AllValues.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("afield1", "afieldvalue1"),
            new KeyValuePair<string, string>("afield2", "afieldvalue2"));
        result.Value.StartedAt.Should().BeNear(DateTime.UtcNow);
        result.Value.CompletedAt.Should().BeNone();
        result.Value.CompletedBy.Should().BeNone();
        result.Value.EnteredAt.Should().BeNear(DateTime.UtcNow);
        result.Value.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void WhenUpdateStepValuesWithExistingKey_ThenUpdatesValue()
    {
        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid1",
            Journey.Empty,
            Journey.Empty,
            100,
            0,
            StringNameValues.Create(new Dictionary<string, string>
            {
                { "aname1", "avalue1" },
                { "aname2", "avalue2" }
            }).Value,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result = state.UpdateCurrentStepValues(StringNameValues
            .Create(new Dictionary<string, string>
            {
                { "aname1", "anothervalue" }
            }).Value);

        result.Should().BeSuccess();
        result.Value.AllValues.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "anothervalue"),
            new KeyValuePair<string, string>("aname2", "avalue2"));
    }

    [Fact]
    public void WhenUpdateStepValues_ThenUpdatesValuesOfLastStepInJourney()
    {
        var step = Step.Create("astepid1", "atitle", 10, DateTime.UtcNow, Optional<DateTime>.None,
            StringNameValues.Create(new Dictionary<string, string> { { "aname1", "avalue1" } }).Value).Value;
        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid1",
            Journey.Create([step]).Value,
            Journey.Empty,
            100,
            0,
            StringNameValues.Create(new Dictionary<string, string> { { "aname1", "avalue1" } }).Value,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result =
            state.UpdateCurrentStepValues(StringNameValues
                .Create(new Dictionary<string, string> { { "aname2", "avalue2" } })
                .Value);

        result.Should().BeSuccess();
        result.Value.PathTaken.Steps.Should().OnlyContain(item => item.StepId == "astepid1"
                                                                  && item.Values.Items["aname1"] == "avalue1"
                                                                  && item.Values.Items["aname2"] == "avalue2");
    }

    [Fact]
    public void WhenUpdateStepValues_ThenOnlyUpdatesCurrentStepValues()
    {
        var state = CurrentStepState.Create(
            OnboardingStatus.InProgress,
            "astepid1",
            Journey.Empty,
            Journey.Empty,
            100,
            0,
            StringNameValues.Create(new Dictionary<string, string> { { "aname1", "avalue1" } }).Value,
            DateTime.UtcNow,
            Optional<DateTime>.None,
            Optional<string>.None).Value;

        var result =
            state.UpdateCurrentStepValues(StringNameValues
                .Create(new Dictionary<string, string> { { "aname2", "avalue2" } })
                .Value);

        result.Should().BeSuccess();
        result.Value.AllValues.Items.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"));
        result.Value.PathTaken.Steps.Should().BeEmpty();
        result.Value.PathAhead.Steps.Should().BeEmpty();
    }

    private static WorkflowSchema CreateTwoStepWorkflow(IOnboardingWorkflowService workflowService)
    {
        return WorkflowSchema.Create(workflowService, "aname", new Dictionary<string, StepSchema>
        {
            {
                "astartstepid", StepSchema.Create(
                    "astartstepid",
                    OnboardingStepType.Start,
                    "astarttitle",
                    Optional<string>.None,
                    "astepid1".ToOptional(),
                    new List<BranchSchema>(),
                    40,
                    new Dictionary<string, string>
                    {
                        { "aname1", "avalue1" }
                    }).Value
            },
            {
                "astepid1", StepSchema.Create(
                    "astepid1",
                    OnboardingStepType.Normal,
                    "asteptitle1",
                    Optional<string>.None,
                    "astepid2".ToOptional(),
                    new List<BranchSchema>(),
                    30,
                    new Dictionary<string, string>
                    {
                        { "aname2", "avalue2" }
                    }).Value
            },
            {
                "astepid2", StepSchema.Create(
                    "astepid2",
                    OnboardingStepType.Normal,
                    "asteptitle2",
                    Optional<string>.None,
                    "anendstepid".ToOptional(),
                    new List<BranchSchema>(),
                    30,
                    new Dictionary<string, string>
                    {
                        { "aname3", "avalue3" }
                    }).Value
            },
            {
                "anendstepid", StepSchema.Create("anendstepid",
                    OnboardingStepType.End,
                    "asteptitle",
                    Optional<string>.None,
                    Optional<string>.None,
                    new List<BranchSchema>(),
                    0,
                    new Dictionary<string, string>
                    {
                        { "aname4", "avalue4" }
                    }).Value
            }
        }, "astartstepid", "anendstepid").Value;
    }
}