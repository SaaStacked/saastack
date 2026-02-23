using Common;
using OrganizationsDomain;
using OrganizationsDomain.DomainServices;

namespace OrganizationsInfrastructure.UnitTests.DomainServices;

public static class Workflows
{
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

    public static StepSchema CreateStartStep(string id, string nextStepId, int weight)
    {
        return StepSchema.Create(id, OnboardingStepType.Start, "astartstep", Optional<string>.None,
            nextStepId, [], weight, new Dictionary<string, string>()).Value;
    }

    public static StepSchema CreateRegularStep(string id, string nextStepId, int weight)
    {
        return StepSchema.Create(id, OnboardingStepType.Normal, "astep", Optional<string>.None,
            nextStepId, [], weight, new Dictionary<string, string>()).Value;
    }

    public static StepSchema CreateEndStep(string id)
    {
        return StepSchema.Create(id, OnboardingStepType.End, "anendstep", Optional<string>.None,
            Optional<string>.None, [], 0, new Dictionary<string, string>()).Value;
    }

    public static StepSchema CreateBranchStep(string id, string field, string value, string nextStepId, int weight)
    {
        var condition = BranchConditionSchema.Create(BranchConditionOperator.Equals, field, value).Value;
        var branch = BranchSchema.Create("abranchstepid", "abranchstep", condition, nextStepId).Value;
        return StepSchema.Create(id, OnboardingStepType.Branch, "atitle", Optional<string>.None,
            Optional<string>.None, [branch], weight, new Dictionary<string, string>()).Value;
    }
}