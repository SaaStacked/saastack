using Application.Persistence.Common;
using Common;
using OrganizationsDomain;
using QueryAny;

namespace OrganizationsApplication.Persistence.ReadModels;

[EntityName("Onboarding")]
public class Onboarding : ReadModelEntity
{
    public Optional<Dictionary<string, string>> AllValues { get; set; }

    public Optional<string> CompletedBy { get; set; }

    public Optional<string> CurrentStepId { get; set; }

    public Optional<string> InitiatedById { get; set; }

    public Optional<string> NavigatedById { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public Optional<string> PreviousStepId { get; set; }

    public OnboardingStatus Status { get; set; }
}