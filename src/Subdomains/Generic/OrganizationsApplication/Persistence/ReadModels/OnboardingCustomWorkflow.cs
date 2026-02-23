using Application.Persistence.Common;
using Common;
using OrganizationsDomain;
using QueryAny;

namespace OrganizationsApplication.Persistence.ReadModels;

[EntityName("OnboardingCustomWorkflow")]
public class OnboardingCustomWorkflow : ReadModelEntity
{
    public Optional<string> EndStepId { get; set; }

    public Optional<JourneySchema> Journey { get; set; }

    public Optional<string> Name { get; set; }

    public Optional<string> StartStepId { get; set; }
}