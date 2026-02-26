using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using AsyncKeyedLock;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Shared;
using OrganizationsApplication.ApplicationServices;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using OrganizationsDomain.DomainServices;

namespace OrganizationsApplication;

public class OnboardingApplication : IOnboardingApplication
{
    private static readonly AsyncKeyedLocker<string> InitiateOnboardingSection = new();
    private readonly ICustomOnboardingWorkflowService _customOnboardingWorkflowService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IRecorder _recorder;
    private readonly IUserProfilesService _userProfilesService;
    private readonly IOnboardingWorkflowService _workflowService;

    public OnboardingApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        ICustomOnboardingWorkflowService customOnboardingWorkflowService, IOnboardingWorkflowService workflowService,
        IUserProfilesService userProfilesService, IOrganizationRepository organizationRepository,
        IOnboardingRepository onboardingRepository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _customOnboardingWorkflowService = customOnboardingWorkflowService;
        _workflowService = workflowService;
        _userProfilesService = userProfilesService;
        _organizationRepository = organizationRepository;
        _onboardingRepository = onboardingRepository;
    }

    public async Task<Result<OrganizationOnboardingWorkflow, Error>> CompleteOnboardingAsync(ICallerContext caller,
        string organizationId, CancellationToken cancellationToken)
    {
        var retrievedOrganization = await _organizationRepository.LoadAsync(organizationId.ToId(), cancellationToken);
        if (retrievedOrganization.IsFailure)
        {
            return retrievedOrganization.Error;
        }

        var organization = retrievedOrganization.Value;
        var retrievedOnboarding =
            await _onboardingRepository.FindByOrganizationIdAsync(organizationId.ToId(), cancellationToken);
        if (retrievedOnboarding.IsFailure)
        {
            return retrievedOnboarding.Error;
        }

        if (!retrievedOnboarding.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var onboarding = retrievedOnboarding.Value.Value;
        var completerRoles = Roles.Create(caller.Roles.Tenant);
        if (completerRoles.IsFailure)
        {
            return completerRoles.Error;
        }

        var completed = onboarding.ForceComplete(completerRoles.Value, caller.ToCallerId());
        if (completed.IsFailure)
        {
            return completed.Error;
        }

        var saved = await _onboardingRepository.SaveAsync(onboarding, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        onboarding = saved.Value;
        var initiatorRoles = Roles.Create(caller.Roles.Tenant);
        if (initiatorRoles.IsFailure)
        {
            return initiatorRoles.Error;
        }

        var ended = organization.EndOnboarding(onboarding.Id, initiatorRoles.Value);
        if (ended.IsFailure)
        {
            return ended.Error;
        }

        var savedOrganization = await _organizationRepository.SaveAsync(organization, cancellationToken);
        if (savedOrganization.IsFailure)
        {
            return savedOrganization.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Ended onboarding {Id} for organization {OrganizationId}",
            onboarding.Id, organizationId);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.OrganizationOnboardingCompleted,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.Id, onboarding.OrganizationId },
                { UsageConstants.Properties.TenantId, organizationId },
                { UsageConstants.Properties.OnboardingStepId, onboarding.State.CurrentStepId }
            });

        return onboarding.ToWorkflow();
    }

    public async Task<Result<OrganizationOnboardingWorkflow, Error>> GetOnboardingAsync(ICallerContext caller,
        string organizationId, CancellationToken cancellationToken)
    {
        var retrieved = await _onboardingRepository.FindByOrganizationIdAsync(organizationId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var onboarding = retrieved.Value.Value;
        _recorder.TraceInformation(caller.ToCall(), "Retrieved onboarding {Id} for organization {OrgId}",
            onboarding.Id, organizationId);

        return onboarding.ToWorkflow();
    }

    public async Task<Result<OrganizationOnboardingWorkflow, Error>> InitiateOnboardingAsync(ICallerContext caller,
        string organizationId, OrganizationOnboardingWorkflowSchema workflow, CancellationToken cancellationToken)
    {
        var retrievedOrganization = await _organizationRepository.LoadAsync(organizationId.ToId(), cancellationToken);
        if (retrievedOrganization.IsFailure)
        {
            return retrievedOrganization.Error;
        }

        // Since we are synchronizing the organization onboarding state and the onboarding aggregate, in this unit of work,
        // we need a reliable way to prevent two callers executing the same process in a race condition.
        // Otherwise, we may end up with duplicate workflows, duplicate onboarding aggregates for the same organization
        var organization = retrievedOrganization.Value;
        using (await InitiateOnboardingSection.LockAsync(organizationId, cancellationToken))
        {
            if (organization.OnboardingStatus != OnboardingStatus.NotStarted)
            {
                return Error.EntityExists(Resources.OnboardingApplication_OnboardingAlreadyInitiated);
            }

            var retrievedOnboarding =
                await _onboardingRepository.FindByOrganizationIdAsync(organizationId.ToId(), cancellationToken);
            if (retrievedOnboarding.IsFailure)
            {
                return retrievedOnboarding.Error;
            }

            if (retrievedOnboarding.Value.HasValue)
            {
                return Error.EntityExists(Resources.OnboardingApplication_OnboardingAlreadyInitiated);
            }

            var savedWorkflow =
                await _customOnboardingWorkflowService.SaveWorkflowAsync(organizationId.ToId(), workflow,
                    cancellationToken);
            if (savedWorkflow.IsFailure)
            {
                return savedWorkflow.Error;
            }

            var created = OrganizationOnboardingRoot.Create(_recorder, _identifierFactory,
                _workflowService, organizationId.ToId());
            if (created.IsFailure)
            {
                return created.Error;
            }

            var onboarding = created.Value;
            var saved = await _onboardingRepository.SaveAsync(onboarding, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            onboarding = saved.Value;
            var initiatorRoles = Roles.Create(caller.Roles.Tenant);
            if (initiatorRoles.IsFailure)
            {
                return initiatorRoles.Error;
            }

            var started = organization.StartOnboarding(onboarding.Id, initiatorRoles.Value);
            if (started.IsFailure)
            {
                return started.Error;
            }

            var savedOrganization = await _organizationRepository.SaveAsync(organization, cancellationToken);
            if (savedOrganization.IsFailure)
            {
                return savedOrganization.Error;
            }

            var emailClassification = await GetCallerEmailClassification(caller, cancellationToken);
            if (emailClassification.IsFailure)
            {
                return emailClassification.Error;
            }

            _recorder.TraceInformation(caller.ToCall(), "Started onboarding {Id} for organization {OrganizationId}",
                onboarding.Id, organizationId);
            _recorder.TrackUsage(caller.ToCall(),
                UsageConstants.Events.UsageScenarios.Generic.OrganizationOnboardingStarted,
                new Dictionary<string, object>
                {
                    { UsageConstants.Properties.Id, onboarding.OrganizationId },
                    { UsageConstants.Properties.TenantId, organizationId },
                    { UsageConstants.Properties.OnboardingStepId, onboarding.State.CurrentStepId },
                    { UsageConstants.Properties.EmailClassification, emailClassification }
                });

            return onboarding.ToWorkflow();
        }
    }

    public async Task<Result<OrganizationOnboardingWorkflow, Error>> MoveBackwardAsync(ICallerContext caller,
        string organizationId, CancellationToken cancellationToken)
    {
        var retrieved = await _onboardingRepository.FindByOrganizationIdAsync(organizationId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var onboarding = retrieved.Value.Value;
        var navigatorRoles = Roles.Create(caller.Roles.Tenant);
        if (navigatorRoles.IsFailure)
        {
            return navigatorRoles.Error;
        }

        var moved = onboarding.MoveBackward(caller.ToCallerId(), navigatorRoles.Value);
        if (moved.IsFailure)
        {
            return moved.Error;
        }

        var saved = await _onboardingRepository.SaveAsync(onboarding, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        onboarding = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Moved onboarding {Id} backward for organization {OrgId}",
            onboarding.Id, organizationId);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.OrganizationOnboardingStepChanged,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.Id, onboarding.OrganizationId },
                { UsageConstants.Properties.TenantId, organizationId },
                { UsageConstants.Properties.OnboardingStepId, onboarding.State.CurrentStepId }
            });

        return onboarding.ToWorkflow();
    }

    public async Task<Result<OrganizationOnboardingWorkflow, Error>> MoveForwardAsync(ICallerContext caller,
        string organizationId, string? nextStepId, Dictionary<string, string>? stepValues,
        CancellationToken cancellationToken)
    {
        var retrieved = await _onboardingRepository.FindByOrganizationIdAsync(organizationId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var onboarding = retrieved.Value.Value;
        var modifierRoles = Roles.Create(caller.Roles.Tenant);
        if (modifierRoles.IsFailure)
        {
            return modifierRoles.Error;
        }

        if (stepValues.HasAny())
        {
            var values = StringNameValues.Create(stepValues!);
            if (values.IsFailure)
            {
                return values.Error;
            }

            var updated = onboarding.UpdateCurrentStep(modifierRoles.Value, values.Value);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            var savedUpdates = await _onboardingRepository.SaveAsync(onboarding, true, cancellationToken);
            if (savedUpdates.IsFailure)
            {
                return savedUpdates.Error;
            }

            onboarding = savedUpdates.Value;
        }

        var moved = onboarding.MoveForward(caller.ToCallerId(), modifierRoles.Value, nextStepId);
        if (moved.IsFailure)
        {
            return moved.Error;
        }

        var nextStep = moved.Value;
        var saved = await _onboardingRepository.SaveAsync(onboarding, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        onboarding = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Moved onboarding {Id} forward to step {StepId} for organization {OrgId}",
            onboarding.Id, nextStep, organizationId);
        _recorder.TrackUsage(caller.ToCall(),
            UsageConstants.Events.UsageScenarios.Generic.OrganizationOnboardingStepChanged,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.Id, onboarding.OrganizationId },
                { UsageConstants.Properties.TenantId, organizationId },
                { UsageConstants.Properties.OnboardingStepId, onboarding.State.CurrentStepId }
            });

        return onboarding.ToWorkflow();
    }

#if TESTINGONLY
    public async Task<Result<OrganizationOnboardingWorkflow, Error>> ResetWorkflowAsync(ICallerContext caller,
        string organizationId, CancellationToken cancellationToken)
    {
        var retrievedOrganization = await _organizationRepository.LoadAsync(organizationId.ToId(), cancellationToken);
        if (retrievedOrganization.IsFailure)
        {
            return retrievedOrganization.Error;
        }

        var organization = retrievedOrganization.Value;

        var resetOrganization = organization.ResetOnboarding();
        if (resetOrganization.IsFailure)
        {
            return resetOrganization.Error;
        }

        var savedOrganization = await _organizationRepository.SaveAsync(organization, cancellationToken);
        if (savedOrganization.IsFailure)
        {
            return savedOrganization.Error;
        }

        var retrievedOnboarding =
            await _onboardingRepository.FindByOrganizationIdAsync(organizationId.ToId(), cancellationToken);
        if (retrievedOnboarding.IsFailure)
        {
            return retrievedOnboarding.Error;
        }

        if (!retrievedOnboarding.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var onboarding = retrievedOnboarding.Value.Value;

        var reset = onboarding.Destroy();
        if (reset.IsFailure)
        {
            return reset.Error;
        }

        var savedOnboarding = await _onboardingRepository.SaveAsync(onboarding, cancellationToken);
        if (savedOnboarding.IsFailure)
        {
            return savedOnboarding.Error;
        }

        onboarding = savedOnboarding.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Reset current onboarding {Id} for organization {OrgId}",
            onboarding.Id, organizationId);

        return onboarding.ToWorkflow();
    }
#endif

    public async Task<Result<OrganizationOnboardingWorkflow, Error>> UpdateCurrentStepAsync(ICallerContext caller,
        string organizationId, Dictionary<string, string> stepValues, CancellationToken cancellationToken)
    {
        var retrieved = await _onboardingRepository.FindByOrganizationIdAsync(organizationId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var onboarding = retrieved.Value.Value;
        var modifierRoles = Roles.Create(caller.Roles.Tenant);
        if (modifierRoles.IsFailure)
        {
            return modifierRoles.Error;
        }

        var values = StringNameValues.Create(stepValues);
        if (values.IsFailure)
        {
            return values.Error;
        }

        var updated = onboarding.UpdateCurrentStep(modifierRoles.Value, values.Value);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        var saved = await _onboardingRepository.SaveAsync(onboarding, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        onboarding = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Updated current step state for onboarding {Id} for organization {OrgId}",
            onboarding.Id, organizationId);

        return onboarding.ToWorkflow();
    }

    private async Task<Result<UserProfileEmailAddressClassification, Error>> GetCallerEmailClassification(
        ICallerContext caller, CancellationToken cancellationToken)
    {
        var retrieved = await _userProfilesService.GetProfilePrivateAsync(caller, caller.CallerId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return UserProfileEmailAddressClassification.Personal;
        }

        var profile = retrieved.Value;
        if (profile.EmailAddress.NotExists())
        {
            return UserProfileEmailAddressClassification.Personal;
        }

        return profile.EmailAddress.Classification;
    }
}

internal static class OnboardingConversionExtensions
{
    public static OrganizationOnboardingWorkflow ToWorkflow(this OrganizationOnboardingRoot onboarding)
    {
        return new OrganizationOnboardingWorkflow
        {
            Id = onboarding.Id,
            OrganizationId = onboarding.OrganizationId,
            Workflow = onboarding.Workflow.ToWorkflowSchema(),
            State = onboarding.State.ToState(onboarding.Workflow)
        };
    }

    private static OrganizationOnboardingWorkflowSchema ToWorkflowSchema(this WorkflowSchema workflow)
    {
        return new OrganizationOnboardingWorkflowSchema
        {
            Name = workflow.Name,
            Steps = workflow.Journeys.Steps.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToStepSchema()),
            StartStepId = workflow.StartStepId,
            EndStepId = workflow.EndStepId
        };
    }

    private static OrganizationOnboardingStepSchema ToStepSchema(this StepSchema step)
    {
        return new OrganizationOnboardingStepSchema
        {
            Id = step.Id,
            Type = step.Type.ToEnumOrDefault(OrganizationOnboardingStepSchemaType.Start),
            Title = step.Title,
            Description = step.Description.ValueOrDefault,
            NextStepId = step.NextStepId.ValueOrDefault,
            Branches = step.Branches.Items.Select(b => b.ToBranchSchema()).ToList(),
            Weight = step.Weight,
            InitialValues = step.InitialValues.Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    private static OrganizationOnboardingBranchSchema ToBranchSchema(this BranchSchema branch)
    {
        return new OrganizationOnboardingBranchSchema
        {
            Id = branch.Id,
            Label = branch.Label,
            Condition = branch.Condition.ToConditionSchema(),
            NextStepId = branch.NextStepId
        };
    }

    private static OrganizationOnboardingBranchConditionSchema ToConditionSchema(this BranchConditionSchema condition)
    {
        return new OrganizationOnboardingBranchConditionSchema
        {
            Operator = condition.Operator.ToEnumOrDefault(OrganizationOnboardingBranchConditionSchemaOperator.Equals),
            Field = condition.Field,
            Value = condition.Value
        };
    }

    private static OrganizationOnboardingState ToState(this CurrentStepState currentStepState, WorkflowSchema workflow)
    {
        var currentStep = workflow.CreateStateStep(currentStepState.CurrentStepId).Value;
        return new OrganizationOnboardingState
        {
            Status = currentStepState.Status.ToEnumOrDefault(OrganizationOnboardingStatus.NotStarted),
            CurrentStep = currentStep.ToStep(),
            PathTaken = currentStepState.PathTaken.Steps.Select(p => p.ToStep()).ToList(),
            PathAhead = currentStepState.PathAhead.Steps.Select(p => p.ToStep()).ToList(),
            TotalWeight = currentStepState.TotalWeight,
            CompletedWeight = currentStepState.CompletedWeight,
            ProgressPercentage = currentStepState.ProgressPercentage,
            Values = currentStepState.AllValues.Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            StartedAt = currentStepState.HasValue()
                ? currentStepState.StartedAt.Value
                : DateTime.UtcNow,
            CompletedAt = currentStepState.CompletedAt.ToNullable(),
            CompletedBy = currentStepState.CompletedBy
        };
    }

    private static OrganizationOnboardingStep ToStep(this Step step)
    {
        return new OrganizationOnboardingStep
        {
            Id = step.StepId,
            Title = step.Title,
            Weight = step.Weight,
            EnteredAt = step.EnteredAt.ToNullable(),
            LastUpdatedAt = step.LastUpdatedAt.ToNullable(),
            Values = step.Values.Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }
}