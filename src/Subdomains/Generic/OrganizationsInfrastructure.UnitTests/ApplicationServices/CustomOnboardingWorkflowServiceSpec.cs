using Application.Resources.Shared;
using Common;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Moq;
using OrganizationsApplication.Persistence;
using OrganizationsApplication.Persistence.ReadModels;
using OrganizationsDomain;
using OrganizationsInfrastructure.ApplicationServices;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class CustomOnboardingWorkflowServiceSpec
{
    private readonly CustomOnboardingWorkflowService _customWorkflowService;
    private readonly Mock<IOnboardingCustomWorkflowRepository> _repository;

    public CustomOnboardingWorkflowServiceSpec()
    {
        _repository = new Mock<IOnboardingCustomWorkflowRepository>();
        _customWorkflowService = new CustomOnboardingWorkflowService(_repository.Object);
    }

    [Fact]
    public async Task WhenFindWorkflowAsync_ThenReturnsWorkflow()
    {
        _repository.Setup(r => r.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OnboardingCustomWorkflow
            {
                Id = "anorganizationid",
                Name = "aname",
                StartStepId = "astartstepid",
                EndStepId = "anendstepid",
                Journey = JourneySchema.Create(new Dictionary<string, StepSchema>
                {
                    {
                        "astartstepid", StepSchema.Create(
                            "astartstepid",
                            OnboardingStepType.Start,
                            "astartstep",
                            Optional<string>.None,
                            "anendstepid",
                            [],
                            100,
                            new Dictionary<string, string>()).Value
                    },
                    {
                        "anendstepid", StepSchema.Create(
                            "anendstepid",
                            OnboardingStepType.End,
                            "anendstep",
                            Optional<string>.None,
                            Optional<string>.None,
                            [],
                            0,
                            new Dictionary<string, string>()).Value
                    }
                }).Value
            });

        var result = await _customWorkflowService.FindWorkflowAsync("anorganizationid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be("aname");
        result.Value.StartStepId.Should().Be("astartstepid");
        result.Value.EndStepId.Should().Be("anendstepid");
        result.Value.Journeys.Steps.Count.Should().Be(2);
        _repository.Verify(r => r.LoadAsync("anorganizationid".ToId(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenSaveAsync_ThenSaves()
    {
        _repository.Setup(r => r.SaveAsync(It.IsAny<OnboardingCustomWorkflow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCustomWorkflow customWorkflow, CancellationToken _) => customWorkflow);

        var steps = new Dictionary<string, OrganizationOnboardingStepSchema>
        {
            {
                "astartstepid", new OrganizationOnboardingStepSchema
                {
                    Id = "astartstepid",
                    Type = OrganizationOnboardingStepSchemaType.Start,
                    Title = "Start Step",
                    NextStepId = "anendstepid",
                    Weight = 100,
                    InitialValues = new Dictionary<string, string>(),
                    Branches = new List<OrganizationOnboardingBranchSchema>(),
                    Description = null
                }
            },
            {
                "anendstepid", new OrganizationOnboardingStepSchema
                {
                    Id = "anendstepid",
                    Type = OrganizationOnboardingStepSchemaType.End,
                    Title = "End Step",
                    Weight = 0,
                    InitialValues = new Dictionary<string, string>(),
                    Branches = new List<OrganizationOnboardingBranchSchema>(),
                    Description = null
                }
            }
        };

        var result = await _customWorkflowService.SaveWorkflowAsync("anorganizationid",
            new OrganizationOnboardingWorkflowSchema
            {
                Name = "aname",
                StartStepId = "astartstepid",
                EndStepId = "anendstepid",
                Steps = steps
            }, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<OnboardingCustomWorkflow>(wf =>
            wf.Id == "anorganizationid"
            && wf.Name == "aname"
            && wf.StartStepId == "astartstepid"
            && wf.EndStepId == "anendstepid"
            && wf.Journey.Value.Steps.Count == 2
        ), It.IsAny<CancellationToken>()));
    }
}