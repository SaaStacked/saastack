using Common;
using OrganizationsDomain.DomainServices;

namespace OrganizationsDomain.UnitTests;

public static class Workflows
{
    public static StepSchema CreateEndStep(string id, IReadOnlyDictionary<string, string>? values = null)
    {
        return StepSchema.Create(id, OnboardingStepType.End, "anendstep", Optional<string>.None,
            Optional<string>.None, [], 0, values ?? new Dictionary<string, string>()).Value;
    }

    /// <summary>
    ///     START (w:50) ──> BRANCH (w:0) ──┬──> STEP1 (w:50) ──┬──> STEP3 (w:50) ──> END (w:0)
    ///     astartstepid     abranchstepid  │      astepid1     │
    ///     -                               │       "left"      │
    ///     -                               │                   │
    ///     -                               └──> STEP2 (w:50) ──┘
    ///     -                                    astepid2
    ///     -                                     "right"
    ///     Paths:
    ///     Path 1: START(50) → BRANCH(0) → STEP1(50) → STEP3(50) → END(0) = 100
    ///     Path 2: START(50) → BRANCH(0) → STEP2(50) → STEP3(50) → END(0) = 100
    /// </summary>
    public static WorkflowSchema CreateMultiBranchWorkflow(IOnboardingWorkflowService workflowService)
    {
        var start = StepSchema.Create("astartstepid", OnboardingStepType.Start, "atitle", Optional<string>.None,
            "abranchstepid", [], 50, new Dictionary<string, string>()).Value;
        var condition1 = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue1").Value;
        var branch1 = BranchSchema.Create("branch1", "Left Branch", condition1, "astepid1").Value;
        var condition2 = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue2").Value;
        var branch2 = BranchSchema.Create("branch2", "Right Branch", condition2, "astepid2").Value;
        var branchStep = StepSchema.Create("abranchstepid", OnboardingStepType.Branch, "atitle",
            Optional<string>.None, Optional<string>.None, [branch1, branch2], 0,
            new Dictionary<string, string>()).Value;
        var step1 = StepSchema.Create("astepid1", OnboardingStepType.Normal, "atitle", Optional<string>.None,
            "astepid3", [], 50, new Dictionary<string, string>()).Value;
        var step2 = StepSchema.Create("astepid2", OnboardingStepType.Normal, "atitle", Optional<string>.None,
            "astepid3", [], 50, new Dictionary<string, string>()).Value;
        var step3 = StepSchema.Create("astepid3", OnboardingStepType.Normal, "atitle", Optional<string>.None,
            "anendstepid", [], 50, new Dictionary<string, string>()).Value;
        var end = StepSchema.Create("anendstepid", OnboardingStepType.End, "atitle", Optional<string>.None,
            Optional<string>.None, [], 0, new Dictionary<string, string>()).Value;

        return WorkflowSchema.Create(workflowService, "aname", new Dictionary<string, StepSchema>
        {
            { "astartstepid", start },
            { "abranchstepid", branchStep },
            { "astepid1", step1 },
            { "astepid2", step2 },
            { "astepid3", step3 },
            { "anendstepid", end }
        }, "astartstepid", "anendstepid").Value;
    }

    /// <summary>
    ///     START (w:50) ──> STEP1 (w:50) ──> BRANCH (w:0) ──┬──> STEP2 (w:50) ──> STEP4 (w:50) ──┐
    ///     astartstepid     astepid1         abranchstepid1 │    astepid2         astepid4       │
    ///     -                                                └──> STEP3 (w:50) ───────────────────└──> END (w:0)
    ///     -                                                     astepid3                             anendstepid
    ///     Paths:
    ///     Path 1: START(50) → STEP1(50) → BRANCH(0) → STEP2(50) → STEP4(50) → END(0) = 200
    ///     Path 2: START(50) → STEP1(50) → BRANCH(0) → STEP3(50) → STEP4(50) → END(0) = 200
    /// </summary>
    public static WorkflowSchema CreateMultiBranchWorkflow2(IOnboardingWorkflowService workflowService)
    {
        var start = CreateStartStep("astartstepid", "astepid1", 50);
        var step1 = CreateRegularStep("astepid1", "abranchstepid1", 50);
        var condition1 = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue1").Value;
        var branch1 = BranchSchema.Create("abranchid1", "left", condition1, "astepid2").Value;
        var condition2 = BranchConditionSchema.Create(BranchConditionOperator.Equals, "afield", "avalue2").Value;
        var branch2 = BranchSchema.Create("branchid2", "right", condition2, "astepid3").Value;
        var branchStep = StepSchema.Create("abranchstepid1", OnboardingStepType.Branch, "atitle",
            "adescription", Optional<string>.None, [branch1, branch2], 0,
            new Dictionary<string, string>()).Value;
        var step2 = CreateRegularStep("astepid2", "astepid4", 50);
        var step3 = CreateRegularStep("astepid3", "astepid4", 50);
        var step4 = CreateRegularStep("astepid4", "anendstepid", 50);
        var end = CreateEndStep("anendstepid");
        var steps = new Dictionary<string, StepSchema>
        {
            { "astartstepid", start },
            { "astepid1", step1 },
            { "abranchstepid1", branchStep },
            { "astepid2", step2 },
            { "astepid3", step3 },
            { "astepid4", step4 },
            { "anendstepid", end }
        };

        return WorkflowSchema.Create(workflowService, "aname", steps, "astartstepid", "anendstepid").Value;
    }

    public static StepSchema CreateRegularStep(string id, string nextStepId, int weight,
        IReadOnlyDictionary<string, string>? values = null)
    {
        return StepSchema.Create(id, OnboardingStepType.Normal, "astep", Optional<string>.None,
            nextStepId, [], weight, values ?? new Dictionary<string, string>()).Value;
    }

    /// <summary>
    ///     START (w:100) ──> END (w:0) ──┐
    ///     astartstepid     anendstepid
    ///     -
    ///     Paths:
    ///     Path 1: START(100) END(0) = 100
    /// </summary>
    public static WorkflowSchema CreateSimplestWorkflow(IOnboardingWorkflowService workflowService)
    {
        var start = CreateStartStep("astartstepid", "anendstepid", 100);
        var end = CreateEndStep("anendstepid");
        return WorkflowSchema.Create(workflowService, "aname", new Dictionary<string, StepSchema>
        {
            { "astartstepid", start },
            { "anendstepid", end }
        }, "astartstepid", "anendstepid").Value;
    }

    /// <summary>
    ///     START (w:50) ──> STEP1 (w:50) ──> END (w:0)
    ///     astartstepid     astepid1         anendstepid
    ///     -
    ///     Paths:
    ///     Path 1: START(50) → STEP1(50) → END(0) = 100
    /// </summary>
    public static WorkflowSchema CreateSingleStepLinearWorkflow(IOnboardingWorkflowService workflowService)
    {
        var start = StepSchema.Create("astartstepid", OnboardingStepType.Start, "atitle", Optional<string>.None,
            "astepid1", [], 50, new Dictionary<string, string> { { "aname1", "avalue1" } }).Value;
        var step1 = StepSchema.Create("astepid1", OnboardingStepType.Normal, "atitle", Optional<string>.None,
            "anendstepid", [], 50, new Dictionary<string, string> { { "aname2", "avalue2" } }).Value;
        var end = StepSchema.Create("anendstepid", OnboardingStepType.End, "atitle", Optional<string>.None,
            Optional<string>.None, [], 0, new Dictionary<string, string> { { "aname3", "avalue3" } }).Value;
        return WorkflowSchema.Create(workflowService, "aname", new Dictionary<string, StepSchema>
        {
            { "astartstepid", start },
            { "astepid1", step1 },
            { "anendstepid", end }
        }, "astartstepid", "anendstepid").Value;
    }

    public static StepSchema CreateStartStep(string id, string nextStepId, int weight,
        IReadOnlyDictionary<string, string>? values = null)
    {
        return StepSchema.Create(id, OnboardingStepType.Start, "astartstep", Optional<string>.None,
            nextStepId, [], weight, values ?? new Dictionary<string, string>()).Value;
    }

    /// <summary>
    ///     START (w:40) ──> STEP1 (w:30) ──> STEP2 (w:30) ──> END (w:0)
    ///     astartstepid     astepid1         astepid2         anendstepid
    ///     -
    ///     Paths:
    ///     Path 1: START(40) → STEP1(30) → STEP2(30) → END(0) = 100
    /// </summary>
    public static WorkflowSchema CreateTwoStepLinearWorkflow(IOnboardingWorkflowService workflowService)
    {
        var start = StepSchema.Create("astartstepid", OnboardingStepType.Start, "atitle", Optional<string>.None,
            "astepid1", [], 40, new Dictionary<string, string> { { "aname1", "avalue1" } }).Value;
        var step1 = StepSchema.Create("astepid1", OnboardingStepType.Normal, "atitle", Optional<string>.None,
            "astepid2", [], 30, new Dictionary<string, string> { { "aname2", "avalue2" } }).Value;
        var step2 = StepSchema.Create("astepid2", OnboardingStepType.Normal, "atitle", Optional<string>.None,
            "anendstepid", [], 30, new Dictionary<string, string> { { "aname3", "avalue3" } }).Value;
        var end = StepSchema.Create("anendstepid", OnboardingStepType.End, "atitle", Optional<string>.None,
            Optional<string>.None, [], 0, new Dictionary<string, string>()).Value;
        return WorkflowSchema.Create(workflowService, "aname", new Dictionary<string, StepSchema>
        {
            { "astartstepid", start },
            { "astepid1", step1 },
            { "astepid2", step2 },
            { "anendstepid", end }
        }, "astartstepid", "anendstepid").Value;
    }
}