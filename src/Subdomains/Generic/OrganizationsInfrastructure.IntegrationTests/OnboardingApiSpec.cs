using ApiHost1;
using Application.Resources.Shared;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace OrganizationsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class OnboardingApiSpec : WebApiSpec<Program>
{
    public OnboardingApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenInitiateOnboardingForPersonalEmailUser_ThenStarts()
    {
        var login = await LoginUserAsync("auser@personal.com", "afirstname");

        var result = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateSimplestWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Workflow.InitiatedById.Should().Be(login.User.Id);
        var state = result.Content.Value.Workflow.State!;
        state.Status.Should().Be(OrganizationOnboardingStatus.InProgress);
        state.TotalWeight.Should().Be(100);
        state.Values.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"));
    }

    [Fact]
    public async Task WhenCompleteOnboardingForPersonalEmailUser_ThenCompletes()
    {
        var login = await LoginUserAsync("auser@personal.com", "afirstname");

        var started = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateSimplestWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        started.Content.Value.Workflow.State!.Status.Should().Be(OrganizationOnboardingStatus.InProgress);

        var result = await Api.PutAsync(new CompleteOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var state = result.Content.Value.Workflow.State!;
        state.Status.Should().Be(OrganizationOnboardingStatus.Complete);
        state.CompletedAt.Should().BeNear(DateTime.UtcNow);
        state.CompletedBy.Should().Be(login.User.Id);
        state.Values.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"));
    }

    [Fact]
    public async Task WhenInitiateOnboardingForCompanyEmailUser_ThenStarts()
    {
        var login = await LoginUserAsync();

        var result = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateSimplestWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Workflow.InitiatedById.Should().Be(login.User.Id);
        var state = result.Content.Value.Workflow.State!;
        state.Status.Should().Be(OrganizationOnboardingStatus.InProgress);
        state.TotalWeight.Should().Be(100);
        state.Values.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"));
    }

    [Fact]
    public async Task WhenCompleteOnboardingForCompanyEmailUser_ThenCompletes()
    {
        var login = await LoginUserAsync();

        var started = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateSimplestWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        started.Content.Value.Workflow.State!.Status.Should().Be(OrganizationOnboardingStatus.InProgress);

        var result = await Api.PutAsync(new CompleteOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var state = result.Content.Value.Workflow.State!;
        state.Status.Should().Be(OrganizationOnboardingStatus.Complete);
        state.CompletedAt.Should().BeNear(DateTime.UtcNow);
        state.CompletedBy.Should().Be(login.User.Id);
        state.Values.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"));
    }

    [Fact]
    public async Task WhenUpdateStartStepWithNewValues_ThenUpdates()
    {
        var login = await LoginUserAsync();

        var started = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateOneStepWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var startedState = started.Content.Value.Workflow.State!;
        startedState.CurrentStep.Id.Should().Be("astartstepid");
        startedState.Values.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"));

        var updated = await Api.PutAsync(new UpdateCurrentWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!,
            Values = new Dictionary<string, string> { { "afield1", "afieldvalue1" }, { "afield2", "afieldvalue2" } }
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var updatedState = updated.Content.Value.Workflow.State!;
        updatedState.CurrentStep.Id.Should().Be("astartstepid");
        updatedState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("afield1", "afieldvalue1"),
            new KeyValuePair<string, string>("afield2", "afieldvalue2"));
    }

    [Fact]
    public async Task WhenMoveForwardAndUpdateAllStepValues_ThenUpdates()
    {
        var login = await LoginUserAsync();

        var started = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateOneStepWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var startedState = started.Content.Value.Workflow.State!;
        startedState.CurrentStep.Id.Should().Be("astartstepid");
        startedState.CompletedWeight.Should().Be(0);
        startedState.ProgressPercentage.Should().Be(0);
        startedState.Values.Should().ContainInOrder(new KeyValuePair<string, string>("aname1", "avalue1"));

        var updatedStartStep = await Api.PutAsync(new UpdateCurrentWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!,
            Values = new Dictionary<string, string> { { "afield11", "afieldvalue11" }, { "afield12", "afieldvalue12" } }
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var updatedStartStepState = updatedStartStep.Content.Value.Workflow.State!;
        updatedStartStepState.CurrentStep.Id.Should().Be("astartstepid");
        updatedStartStepState.CompletedWeight.Should().Be(0);
        updatedStartStepState.ProgressPercentage.Should().Be(0);
        updatedStartStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("afield11", "afieldvalue11"),
            new KeyValuePair<string, string>("afield12", "afieldvalue12"));

        var firstStepMoved = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!,
            NextStepId = "astepid1"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var firstStepMovedState = firstStepMoved.Content.Value.Workflow.State!;
        firstStepMovedState.CurrentStep.Id.Should().Be("astepid1");
        firstStepMovedState.CompletedWeight.Should().Be(33);
        firstStepMovedState.ProgressPercentage.Should().Be(33);
        firstStepMovedState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("afield11", "afieldvalue11"),
            new KeyValuePair<string, string>("afield12", "afieldvalue12"),
            new KeyValuePair<string, string>("aname2", "avalue2"));

        var updatedFirstStep = await Api.PutAsync(new UpdateCurrentWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!,
            Values = new Dictionary<string, string> { { "afield21", "afieldvalue21" }, { "afield22", "afieldvalue22" } }
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var updatedFirstStepState = updatedFirstStep.Content.Value.Workflow.State!;
        updatedFirstStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("afield11", "afieldvalue11"),
            new KeyValuePair<string, string>("afield12", "afieldvalue12"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("afield21", "afieldvalue21"),
            new KeyValuePair<string, string>("afield22", "afieldvalue22"));

        var endStepMoved = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!,
            NextStepId = "anendstepid"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var endStepMovedState = endStepMoved.Content.Value.Workflow.State!;
        endStepMovedState.CurrentStep.Id.Should().Be("anendstepid");
        endStepMovedState.CompletedWeight.Should().Be(100);
        endStepMovedState.ProgressPercentage.Should().Be(100);
        endStepMovedState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("afield11", "afieldvalue11"),
            new KeyValuePair<string, string>("afield12", "afieldvalue12"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("afield21", "afieldvalue21"),
            new KeyValuePair<string, string>("afield22", "afieldvalue22"),
            new KeyValuePair<string, string>("aname3", "avalue3"));

        var updatedEndStep = await Api.PutAsync(new UpdateCurrentWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!,
            Values = new Dictionary<string, string> { { "afield31", "afieldvalue31" }, { "afield32", "afieldvalue32" } }
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var updatedEndStepState = updatedEndStep.Content.Value.Workflow.State!;
        updatedEndStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("afield11", "afieldvalue11"),
            new KeyValuePair<string, string>("afield12", "afieldvalue12"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("afield21", "afieldvalue21"),
            new KeyValuePair<string, string>("afield22", "afieldvalue22"),
            new KeyValuePair<string, string>("aname3", "avalue3"),
            new KeyValuePair<string, string>("afield31", "afieldvalue31"),
            new KeyValuePair<string, string>("afield32", "afieldvalue32"));

        var complete = await Api.PutAsync(new CompleteOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var completeState = complete.Content.Value.Workflow.State!;
        completeState.CompletedWeight.Should().Be(100);
        completeState.ProgressPercentage.Should().Be(100);
        completeState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("afield11", "afieldvalue11"),
            new KeyValuePair<string, string>("afield12", "afieldvalue12"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("afield21", "afieldvalue21"),
            new KeyValuePair<string, string>("afield22", "afieldvalue22"),
            new KeyValuePair<string, string>("aname3", "avalue3"),
            new KeyValuePair<string, string>("afield31", "afieldvalue31"),
            new KeyValuePair<string, string>("afield32", "afieldvalue32"));
    }

    [Fact]
    public async Task WhenMoveForwardsAndMoveBackwardsToFirstStep_ThenNavigates()
    {
        var login = await LoginUserAsync();

        var started = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateOneStepWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var startedState = started.Content.Value.Workflow.State!;
        startedState.CurrentStep.Id.Should().Be("astartstepid");
        startedState.CurrentStep.Weight.Should().Be(33);
        startedState.CompletedWeight.Should().Be(0);
        startedState.ProgressPercentage.Should().Be(0);

        var movedToFirstStep = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var movedToFirstStepState = movedToFirstStep.Content.Value.Workflow.State!;
        movedToFirstStepState.CurrentStep.Id.Should().Be("astepid1");
        movedToFirstStepState.CurrentStep.Weight.Should().Be(67);
        movedToFirstStepState.CompletedWeight.Should().Be(33);
        movedToFirstStepState.ProgressPercentage.Should().Be(33);

        var backToStartStep = await Api.PutAsync(new MoveBackWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var backToStartStepState = backToStartStep.Content.Value.Workflow.State!;
        backToStartStepState.CurrentStep.Id.Should().Be("astartstepid");
        backToStartStepState.CompletedWeight.Should().Be(0);
        backToStartStepState.ProgressPercentage.Should().Be(0);

        movedToFirstStep = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        movedToFirstStepState = movedToFirstStep.Content.Value.Workflow.State!;
        movedToFirstStepState.CurrentStep.Id.Should().Be("astepid1");
        movedToFirstStepState.CurrentStep.Weight.Should().Be(67);
        movedToFirstStepState.CompletedWeight.Should().Be(33);
        movedToFirstStepState.ProgressPercentage.Should().Be(33);

        var movedToEndStep = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var movedToEndStepState = movedToEndStep.Content.Value.Workflow.State!;
        movedToEndStepState.CurrentStep.Id.Should().Be("anendstepid");
        movedToEndStepState.CurrentStep.Weight.Should().Be(0);
        movedToEndStepState.CompletedWeight.Should().Be(100);
        movedToEndStepState.ProgressPercentage.Should().Be(100);

        var backToFirstStep = await Api.PutAsync(new MoveBackWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var backToFirstStepState = backToFirstStep.Content.Value.Workflow.State!;
        backToFirstStepState.CurrentStep.Id.Should().Be("astepid1");
        backToFirstStepState.CompletedWeight.Should().Be(33);
        backToFirstStepState.ProgressPercentage.Should().Be(33);
    }

    [Fact]
    public async Task WhenMoveForwardsAndMoveBackwardsUpAndDownWorkflow_ThenNavigates()
    {
        var login = await LoginUserAsync();

        var started = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateBranchingWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var startedState = started.Content.Value.Workflow.State!;
        startedState.PathAhead.Select(s => s.Id).Should()
            .BeEquivalentTo("branchstepid1", "astepid2", "anendstepid");
        startedState.PathAhead.Select(s => s.Title).Should()
            .BeEquivalentTo("Branch Step", "Step 2 (Branch Left)", "End Step");
        startedState.CurrentStep.Id.Should().Be("astartstepid");
        startedState.CurrentStep.Title.Should().Be("Start Step");
        startedState.CurrentStep.Weight.Should().Be(25);
        startedState.PathTaken.Should().BeEmpty();
        startedState.CompletedWeight.Should().Be(0);
        startedState.ProgressPercentage.Should().Be(0);
        startedState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"));

        // Move down branch to end
        var movedBranchStep = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var movedBranchStepState = movedBranchStep.Content.Value.Workflow.State!;
        movedBranchStepState.PathAhead.Select(s => s.Id).Should().BeEquivalentTo("astepid2", "anendstepid");
        movedBranchStepState.PathAhead.Select(s => s.Title).Should()
            .BeEquivalentTo("Step 2 (Branch Left)", "End Step");
        movedBranchStepState.CurrentStep.Id.Should().Be("branchstepid1");
        movedBranchStepState.CurrentStep.Title.Should().Be("Branch Step");
        movedBranchStepState.CurrentStep.Weight.Should().Be(25);
        movedBranchStepState.PathTaken.Select(s => s.Id).Should().BeEquivalentTo("astartstepid");
        movedBranchStepState.PathTaken.Select(s => s.Title).Should().BeEquivalentTo("Start Step");
        movedBranchStepState.CompletedWeight.Should().Be(25);
        movedBranchStepState.ProgressPercentage.Should().Be(16);
        movedBranchStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"));

        // chooses left branch by default
        var movedToSecondStep = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var movedToSecondStepState = movedToSecondStep.Content.Value.Workflow.State!;
        movedToSecondStepState.PathAhead.Select(s => s.Id).Should().BeEquivalentTo("anendstepid");
        movedToSecondStepState.PathAhead.Select(s => s.Title).Should()
            .BeEquivalentTo("End Step");
        movedToSecondStepState.CurrentStep.Id.Should().Be("astepid2");
        movedToSecondStepState.CurrentStep.Title.Should().Be("Step 2 (Branch Left)");
        movedToSecondStepState.CurrentStep.Weight.Should().Be(50);
        movedToSecondStepState.PathTaken.Select(s => s.Id).Should().BeEquivalentTo("astartstepid", "branchstepid1");
        movedToSecondStepState.PathTaken.Select(s => s.Title).Should().BeEquivalentTo("Start Step", "Branch Step");
        movedToSecondStepState.CompletedWeight.Should().Be(50);
        movedToSecondStepState.ProgressPercentage.Should().Be(33);
        movedToSecondStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3"));

        var movedToEndStep = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var movedToEndStepState = movedToEndStep.Content.Value.Workflow.State!;
        movedToEndStepState.PathAhead.Select(s => s.Id).Should().BeEquivalentTo();
        movedToEndStepState.PathAhead.Select(s => s.Title).Should().BeEquivalentTo();
        movedToEndStepState.CurrentStep.Id.Should().Be("anendstepid");
        movedToEndStepState.CurrentStep.Title.Should().Be("End Step");
        movedToEndStepState.CurrentStep.Weight.Should().Be(0);
        movedToEndStepState.PathTaken.Select(s => s.Id).Should()
            .BeEquivalentTo("astartstepid", "branchstepid1", "astepid2");
        movedToEndStepState.PathTaken.Select(s => s.Title).Should()
            .BeEquivalentTo("Start Step", "Branch Step", "Step 2 (Branch Left)");
        movedToEndStepState.CompletedWeight.Should().Be(100);
        movedToEndStepState.ProgressPercentage.Should().Be(66);
        movedToEndStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3"),
            new KeyValuePair<string, string>("aname5", "avalue5"));

        // Move backward from end to start
        var backToSecondStep = await Api.PutAsync(new MoveBackWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var backToSecondStepState = backToSecondStep.Content.Value.Workflow.State!;
        backToSecondStepState.CurrentStep.Id.Should().Be("astepid2");
        backToSecondStepState.CompletedWeight.Should().Be(50);
        backToSecondStepState.ProgressPercentage.Should().Be(33);
        backToSecondStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3"),
            new KeyValuePair<string, string>("aname5", "avalue5"));

        var backToBranchStep = await Api.PutAsync(new MoveBackWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var backToBranchStepState = backToBranchStep.Content.Value.Workflow.State!;
        backToBranchStepState.CurrentStep.Id.Should().Be("branchstepid1");
        backToBranchStepState.CompletedWeight.Should().Be(25);
        backToBranchStepState.ProgressPercentage.Should().Be(16);
        backToBranchStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3"),
            new KeyValuePair<string, string>("aname5", "avalue5"));

        var backToStartStep = await Api.PutAsync(new MoveBackWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var backToStartStepState = backToStartStep.Content.Value.Workflow.State!;
        backToStartStepState.CurrentStep.Id.Should().Be("astartstepid");
        backToStartStepState.CompletedWeight.Should().Be(0);
        backToStartStepState.ProgressPercentage.Should().Be(0);
        backToStartStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname3", "avalue3"),
            new KeyValuePair<string, string>("aname5", "avalue5"));
    }

    [Fact]
    public async Task WhenMoveForwardsAndMatchingBranchCondition_ThenNavigatesToRightBranchStep()
    {
        var login = await LoginUserAsync();

        var started = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateBranchingWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var startedState = started.Content.Value.Workflow.State!;
        startedState.CurrentStep.Id.Should().Be("astartstepid");
        startedState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"));

        // Move down branch to end
        var movedBranchStep = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var movedBranchStepState = movedBranchStep.Content.Value.Workflow.State!;
        movedBranchStepState.PathAhead.Select(s => s.Id).Should().BeEquivalentTo("astepid2", "anendstepid");
        movedBranchStepState.CurrentStep.Id.Should().Be("branchstepid1");
        movedBranchStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"));

        var movedToNextStep = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!,
            Values = new Dictionary<string, string>
            {
                { "choice", "option2" } //see workflow conditions
            }
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var movedToNextStepState = movedToNextStep.Content.Value.Workflow.State!;
        movedToNextStepState.PathAhead.Select(s => s.Id).Should().BeEquivalentTo("anendstepid");
        movedToNextStepState.CurrentStep.Id.Should().Be("astepid1");
        movedToNextStepState.CurrentStep.Title.Should().Be("Step 1 (Branch Right)");
        movedToNextStepState.CurrentStep.Weight.Should().Be(50);
        movedToNextStepState.PathTaken.Select(s => s.Id).Should().BeEquivalentTo("astartstepid", "branchstepid1");
        movedToNextStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname4", "avalue4"));

        var movedToEndStep = await Api.PutAsync(new MoveForwardWorkflowStepRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var movedToEndStepState = movedToEndStep.Content.Value.Workflow.State!;
        movedToEndStepState.CurrentStep.Id.Should().Be("anendstepid");
        movedToEndStepState.PathTaken.Select(s => s.Id).Should()
            .BeEquivalentTo("astartstepid", "branchstepid1", "astepid1");
        movedToEndStepState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"),
            new KeyValuePair<string, string>("aname2", "avalue2"),
            new KeyValuePair<string, string>("aname4", "avalue4"),
            new KeyValuePair<string, string>("aname5", "avalue5"));
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenResetForInProgress_ThenReset()
    {
        var login = await LoginUserAsync();

        var started = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateBranchingWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var startedState = started.Content.Value.Workflow.State!;
        startedState.CurrentStep.Id.Should().Be("astartstepid");
        startedState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"));

        var organization = await Api.GetAsync(new GetOrganizationRequest
        {
            Id = login.DefaultOrganizationId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var organizationState = organization.Content.Value.Organization;
        organizationState.OnboardingStatus.Should().Be(OrganizationOnboardingStatus.InProgress);

        await Api.PutAsync(new ResetCurrentWorkflowRequest
        {
            Id = login.DefaultOrganizationId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var secondStarted = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateBranchingWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var secondStartedState = secondStarted.Content.Value.Workflow.State!;
        secondStartedState.CurrentStep.Id.Should().Be("astartstepid");
        secondStartedState.Values.Should().ContainInOrder(
            new KeyValuePair<string, string>("aname1", "avalue1"));
    }
#endif

#if TESTINGONLY
    [Fact]
    public async Task WhenResetForCompleted_ThenReset()
    {
        var login = await LoginUserAsync();

        var started = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateBranchingWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var startedState = started.Content.Value.Workflow.State!;
        startedState.CurrentStep.Id.Should().Be("astartstepid");

        await Api.PutAsync(new CompleteOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var organization = await Api.GetAsync(new GetOrganizationRequest
        {
            Id = login.DefaultOrganizationId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var organizationState = organization.Content.Value.Organization;
        organizationState.OnboardingStatus.Should().Be(OrganizationOnboardingStatus.Complete);

        await Api.PutAsync(new ResetCurrentWorkflowRequest
        {
            Id = login.DefaultOrganizationId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var secondStarted = await Api.PostAsync(new InitiateOnboardingWorkflowRequest
        {
            Id = login.DefaultOrganizationId!,
            Workflow = CreateBranchingWorkflow()
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var secondStartedState = secondStarted.Content.Value.Workflow.State!;
        secondStartedState.CurrentStep.Id.Should().Be("astartstepid");
    }
#endif

    private static OrganizationOnboardingWorkflowSchema CreateSimplestWorkflow()
    {
        return new OrganizationOnboardingWorkflowSchema
        {
            Name = "simplest",
            StartStepId = "astartstepid",
            EndStepId = "anendstepid",
            Steps = new Dictionary<string, OrganizationOnboardingStepSchema>
            {
                {
                    "astartstepid", new OrganizationOnboardingStepSchema
                    {
                        Id = "astartstepid",
                        Type = OrganizationOnboardingStepSchemaType.Start,
                        Title = "astartstep",
                        NextStepId = "anendstepid",
                        Weight = 100,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname1", "avalue1" }
                        }
                    }
                },
                {
                    "anendstepid", new OrganizationOnboardingStepSchema
                    {
                        Id = "anendstepid",
                        Type = OrganizationOnboardingStepSchemaType.End,
                        Title = "anendstep",
                        Weight = 0,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname2", "avalue2" }
                        }
                    }
                }
            }
        };
    }

    private static OrganizationOnboardingWorkflowSchema CreateOneStepWorkflow()
    {
        return new OrganizationOnboardingWorkflowSchema
        {
            Name = "threesteps",
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
                        Weight = 33,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname1", "avalue1" }
                        }
                    }
                },
                {
                    "astepid1", new OrganizationOnboardingStepSchema
                    {
                        Id = "astepid1",
                        Type = OrganizationOnboardingStepSchemaType.Normal,
                        Title = "Step 1",
                        NextStepId = "anendstepid",
                        Weight = 67,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname2", "avalue2" }
                        }
                    }
                },
                {
                    "anendstepid", new OrganizationOnboardingStepSchema
                    {
                        Id = "anendstepid",
                        Type = OrganizationOnboardingStepSchemaType.End,
                        Title = "End Step",
                        Weight = 0,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname3", "avalue3" }
                        }
                    }
                }
            }
        };
    }

    private static OrganizationOnboardingWorkflowSchema CreateBranchingWorkflow()
    {
        return new OrganizationOnboardingWorkflowSchema
        {
            Name = "branching",
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
                        NextStepId = "branchstepid1",
                        Weight = 25,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname1", "avalue1" }
                        }
                    }
                },
                {
                    "branchstepid1", new OrganizationOnboardingStepSchema
                    {
                        Id = "branchstepid1",
                        Type = OrganizationOnboardingStepSchemaType.Branch,
                        Title = "Branch Step",
                        Weight = 25,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname2", "avalue2" }
                        },
                        Branches =
                        [
                            new OrganizationOnboardingBranchSchema
                            {
                                Id = "aleftstepid1",
                                Label = "Option 1",
                                NextStepId = "astepid2",
                                Condition = new OrganizationOnboardingBranchConditionSchema
                                {
                                    Field = "choice",
                                    Operator = OrganizationOnboardingBranchConditionSchemaOperator.Equals,
                                    Value = "option1"
                                }
                            },

                            new OrganizationOnboardingBranchSchema
                            {
                                Id = "arightstepid1",
                                Label = "Option 2",
                                NextStepId = "astepid1",
                                Condition = new OrganizationOnboardingBranchConditionSchema
                                {
                                    Field = "choice",
                                    Operator = OrganizationOnboardingBranchConditionSchemaOperator.Equals,
                                    Value = "option2"
                                }
                            }
                        ]
                    }
                },
                {
                    "astepid2", new OrganizationOnboardingStepSchema
                    {
                        Id = "astepid2",
                        Type = OrganizationOnboardingStepSchemaType.Normal,
                        Title = "Step 2 (Branch Left)",
                        NextStepId = "anendstepid",
                        Weight = 50,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname3", "avalue3" }
                        }
                    }
                },
                {
                    "astepid1", new OrganizationOnboardingStepSchema
                    {
                        Id = "astepid1",
                        Type = OrganizationOnboardingStepSchemaType.Normal,
                        Title = "Step 1 (Branch Right)",
                        NextStepId = "anendstepid",
                        Weight = 50,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname4", "avalue4" }
                        }
                    }
                },
                {
                    "anendstepid", new OrganizationOnboardingStepSchema
                    {
                        Id = "anendstepid",
                        Type = OrganizationOnboardingStepSchemaType.End,
                        Title = "End Step",
                        Weight = 0,
                        InitialValues = new Dictionary<string, string>
                        {
                            { "aname5", "avalue5" }
                        }
                    }
                }
            }
        };
    }
}