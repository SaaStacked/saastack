using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class OrganizationOnboardingWorkflowSchema
{
    public string EndStepId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string StartStepId { get; set; } = string.Empty;

    public Dictionary<string, OrganizationOnboardingStepSchema> Steps { get; set; } = new();
}

public enum OrganizationOnboardingStepSchemaType
{
    Start = 0,
    Normal = 1,
    Branch = 2,
    End = 3
}

public class OrganizationOnboardingStepSchema : IIdentifiableResource
{
    public List<OrganizationOnboardingBranchSchema>? Branches { get; set; } = new();

    public string? Description { get; set; }

    public Dictionary<string, string>? InitialValues { get; set; } = new();

    public string? NextStepId { get; set; }

    public string Title { get; set; } = string.Empty;

    public OrganizationOnboardingStepSchemaType Type { get; set; } = OrganizationOnboardingStepSchemaType.Normal;

    public int Weight { get; set; }

    public string Id { get; set; } = string.Empty;
}

public class OrganizationOnboardingBranchSchema : IIdentifiableResource
{
    public OrganizationOnboardingBranchConditionSchema Condition { get; set; } = new();

    public string Label { get; set; } = string.Empty;

    public string NextStepId { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;
}

public class OrganizationOnboardingBranchConditionSchema
{
    public string Field { get; set; } = string.Empty;

    public OrganizationOnboardingBranchConditionSchemaOperator Operator { get; set; } =
        OrganizationOnboardingBranchConditionSchemaOperator.Equals;

    public string Value { get; set; } = string.Empty;
}

public enum OrganizationOnboardingBranchConditionSchemaOperator
{
    Equals = 0,
    Contains = 1,
    GreaterThan = 2,
    LessThan = 3
}

public class OrganizationOnboardingWorkflow : IIdentifiableResource
{
    public required string InitiatedById { get; set; }

    public required string OrganizationId { get; set; }

    public required OrganizationOnboardingState? State { get; set; }

    public required OrganizationOnboardingWorkflowSchema Workflow { get; set; }

    public required string Id { get; set; }
}

public class OrganizationOnboardingState
{
    public DateTime? CompletedAt { get; set; }

    public string? CompletedBy { get; set; }

    public required int CompletedWeight { get; set; }

    public required OrganizationOnboardingStep CurrentStep { get; set; }

    public required List<OrganizationOnboardingStep> PathAhead { get; set; }

    public required List<OrganizationOnboardingStep> PathTaken { get; set; }

    public required int ProgressPercentage { get; set; }

    public required DateTime StartedAt { get; set; }

    public required OrganizationOnboardingStatus Status { get; set; }

    public required int TotalWeight { get; set; }

    public required Dictionary<string, string> Values { get; set; }
}

public class OrganizationOnboardingStep
{
    public DateTime? EnteredAt { get; set; }

    public required string Id { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public required string Title { get; set; }

    public required Dictionary<string, string> Values { get; set; }

    public required int Weight { get; set; }
}