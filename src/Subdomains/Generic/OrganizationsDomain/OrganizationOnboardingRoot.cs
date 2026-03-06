using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Organizations.Onboarding;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using JetBrains.Annotations;
using OrganizationsDomain.DomainServices;

namespace OrganizationsDomain;

/// <summary>
///     This aggregate is an optimization built separately to the <see cref="OrganizationRoot" /> to control only one
///     aspect of
///     organization management - onboarding. We have a 1-1 relationship between <see cref="OrganizationOnboardingRoot" />
///     and <see cref="OrganizationRoot" />, and this aggregate is expected to have a far shorter lifetime than an
///     <see cref="OrganizationRoot" />. The aggregate is also expected to be very chatty, and the
///     <see cref="OrganizationRoot" /> is already dealing with a lot of change over its lifetime.
///     This onboarding aggregate will be dealing with potentially large data sets (i.e., the initial workflow schema, and
///     also the states at each step).
///     Basically, an onboarding process itself has a simple lifecycle (Started -> InProgress -> Complete),
///     and contained within that lifecycle, is a customizable onboarding workflow of the product's choosing.
///     One workflow per instance of a <see cref="OrganizationRoot" />
///     1. You set the initial schema of the workflow, and it is fully validated, and persisted for the organization
///     2. You start the onboarding process (acoriding to your custom schema), and we navigate to the first step.
///     The parent <see cref="OrganizationRoot" /> also tracks that event.
///     3. Each step in the custom workflow has a set of arbitrary properties that can be updated repeatedly.
///     4. These arbitrary propety values (per step) can drive branch conditions, or they can just be used to drive
///     external processes, that are implmented as consumers to the events.
///     5. You navigate to the next step in one of the specified branches of the workflow, or navigate backwards
///     6. You complete the workflow. The parent <see cref="OrganizationRoot" /> also tracks this event.
///     Workflow Notes:
///     The custom workflow is essentially a Directed Acyclical Graph (DAG) of steps (
///     <see cref="OnboardingWorkflowGraphService" />) , that can be traversed either forwards or backwards
///     (not necessarily linearly) and not necessarily only defining a single path.
///     Since this workflow will be followed by humans (who are following the actual onboarding process in the real world)
///     and we want to design an API that is easy to use - we want to be a little more forgiving about the rules of
///     traversal. For example:
///     * You can traverse forwards down any specified branch (in the schema), one or as many steps as you like
///     (essentially skipping all steps on the way)
///     * You can traverse backwards at any time, but only one step at a time.
///     * You can persist the state of any step as many times as you like.
///     * You can complete the workflow at any time (essentially, skipping all steps - in the current branch - all the way
///     to the end)
///     * The workflow can be abandoned at any time, and restarted later.
/// </summary>
public sealed class OrganizationOnboardingRoot : AggregateRootBase
{
    private readonly IOnboardingWorkflowService _workflowService;

    public static Result<OrganizationOnboardingRoot, Error> Create(IRecorder recorder,
        IIdentifierFactory idFactory, IOnboardingWorkflowService workflowService,
        Identifier organizationId, Identifier initiatedBy)
    {
        var root = new OrganizationOnboardingRoot(recorder, idFactory, workflowService);
        root.RaiseCreateEvent(OrganizationsDomain.Events.Onboarding.Created(root.Id, organizationId, initiatedBy));
        return root;
    }

    private OrganizationOnboardingRoot(IRecorder recorder, IIdentifierFactory idFactory,
        IOnboardingWorkflowService workflowService) : base(recorder, idFactory)
    {
        _workflowService = workflowService;
    }

    private OrganizationOnboardingRoot(IRecorder recorder, IIdentifierFactory idFactory,
        IOnboardingWorkflowService workflowService,
        ISingleValueObject<string> identifier) : base(recorder, idFactory, identifier)
    {
        _workflowService = workflowService;
    }

    public Identifier OrganizationId { get; private set; } = Identifier.Empty();

    public Identifier InitiatedById { get; private set; } = Identifier.Empty();

    public CurrentStepState State { get; private set; } = CurrentStepState.Empty;

    public WorkflowSchema Workflow { get; private set; } = WorkflowSchema.Empty;

    [UsedImplicitly]
    public static AggregateRootFactory<OrganizationOnboardingRoot> Rehydrate()
    {
        return (identifier, container, _) => new OrganizationOnboardingRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(),
            container.GetRequiredService<IOnboardingWorkflowService>(),
            identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                InitiatedById = created.InitiatedById.ToId();
                var workflow = _workflowService.FindWorkflow(OrganizationId);
                if (workflow.IsFailure)
                {
                    return workflow.Error;
                }

                Workflow = workflow.Value;
                var state = Workflow.InitiateStart(_workflowService);
                if (state.IsFailure)
                {
                    return state.Error;
                }

                State = state.Value;
                Recorder.TraceDebug(null, "Onboarding {Id} started for organization {OrganizationId} at step {StepId}",
                    Id,
                    OrganizationId, State.CurrentStepId);
                return Result.Ok;
            }

            case StepNavigated changed:
            {
                var state = State.NavigateToStep(_workflowService, changed.FromStepId, changed.ToStepId, Workflow);
                if (state.IsFailure)
                {
                    return state.Error;
                }

                State = state.Value;
                Recorder.TraceDebug(null,
                    "Onboarding {Id} for Organization {OrganizationId} has advanced from step {OldStepId} to step {NewStepId}",
                    Id, OrganizationId, changed.ToStepId, changed.FromStepId);
                return Result.Ok;
            }

            case StepStateChanged changed:
            {
                var values = StringNameValues.Create(changed.Values);
                if (values.IsFailure)
                {
                    return values.Error;
                }

                var state = State.UpdateCurrentStepValues(values.Value);
                if (state.IsFailure)
                {
                    return state.Error;
                }

                State = state.Value;
                Recorder.TraceDebug(null,
                    "Onboarding {Id} for Organization {OrganizationId} has updated the state for step {StepId}", Id,
                    OrganizationId, changed.CurrentStepId);
                return Result.Ok;
            }

            case Completed completed:
            {
                var state = State.MarkComplete(completed.CompletedBy);
                if (state.IsFailure)
                {
                    return state.Error;
                }

                State = state.Value;
                Recorder.TraceDebug(null,
                    "Onboarding {Id} for Organization {OrganizationId} has been completed by {User}", Id,
                    OrganizationId, completed.CompletedBy);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    /// <summary>
    ///     Forces a completion of the onboarding, regardless of where we are in the process.
    ///     We may not be 100% complete.
    /// </summary>
    public Result<Error> ForceComplete(Roles completerRoles, Identifier completerId)
    {
        if (!IsOwner(completerRoles))
        {
            return Error.RoleViolation(Resources.OrganizationOnboardingRoot_UserNotOrgOwner);
        }

        switch (State.Status)
        {
            case OnboardingStatus.NotStarted:
                return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_NotStarted);

            case OnboardingStatus.Complete:
                return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_AlreadyCompleted);

            case OnboardingStatus.InProgress:
            default:
                // desired
                break;
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.Onboarding.Completed(Id, OrganizationId, completerId));
    }

    public Result<Error> MoveBackward(Identifier navigatorId, Roles navigatorRoles)
    {
        if (!IsOwner(navigatorRoles))
        {
            return Error.RoleViolation(Resources.OrganizationOnboardingRoot_UserNotOrgOwner);
        }

        switch (State.Status)
        {
            case OnboardingStatus.NotStarted:
                return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_NotStarted);

            case OnboardingStatus.Complete:
                return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_AlreadyCompleted);

            case OnboardingStatus.InProgress:
            default:
                // desired
                break;
        }

        var currentStepType = GetCurrentStepType();
        if (currentStepType == OnboardingStepType.Start)
        {
            return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_MoveBackward_AtStartStep);
        }

        var previousStepId = State.PathTaken.Steps.Last().StepId;
        return RaiseChangeEvent(OrganizationsDomain.Events.Onboarding.StepNavigated(Id, OrganizationId,
            State.CurrentStepId, previousStepId, navigatorId));
    }

    public Result<string, Error> MoveForward(Identifier navigatorId, Roles navigatorRoles, Optional<string> toStepId)
    {
        if (!IsOwner(navigatorRoles))
        {
            return Error.RoleViolation(Resources.OrganizationOnboardingRoot_UserNotOrgOwner);
        }

        switch (State.Status)
        {
            case OnboardingStatus.NotStarted:
                return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_NotStarted);

            case OnboardingStatus.Complete:
                return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_AlreadyCompleted);

            case OnboardingStatus.InProgress:
            default:
                // desired
                break;
        }

        var currentStepType = GetCurrentStepType();
        if (currentStepType == OnboardingStepType.End)
        {
            return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_MoveForward_AtEndStep);
        }

        string nextStepId;
        if (toStepId.HasValue)
        {
            var validated = Workflow.ValidateForwardMove(State.CurrentStepId, toStepId.Value);
            if (validated.IsFailure)
            {
                return validated.Error;
            }

            nextStepId = toStepId.Value;
        }
        else
        {
            var determined = Workflow.DetermineNextStep(State.CurrentStepId, State.AllValues);
            if (determined.IsFailure)
            {
                return determined.Error;
            }

            nextStepId = determined.Value;
        }

        var raised = RaiseChangeEvent(
            OrganizationsDomain.Events.Onboarding.StepNavigated(Id, OrganizationId, State.CurrentStepId, nextStepId,
                navigatorId));
        if (raised.IsFailure)
        {
            return raised.Error;
        }

        return nextStepId;
    }

#if TESTINGONLY
    public void TestingOnly_SetStatus(OnboardingStatus status)
    {
        State.TestingOnly_SetStatus(status);
    }
#endif

    public Result<Error> UpdateCurrentStep(Roles modifierRoles, StringNameValues values)
    {
        if (!IsOwner(modifierRoles))
        {
            return Error.RoleViolation(Resources.OrganizationOnboardingRoot_UserNotOrgOwner);
        }

        switch (State.Status)
        {
            case OnboardingStatus.NotStarted:
                return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_NotStarted);

            case OnboardingStatus.Complete:
                return Error.PreconditionViolation(Resources.OrganizationOnboardingRoot_AlreadyCompleted);

            case OnboardingStatus.InProgress:
            default:
                // desired
                break;
        }

        return RaiseChangeEvent(
            OrganizationsDomain.Events.Onboarding.StepStateChanged(Id, OrganizationId, State.CurrentStepId, values));
    }

    private OnboardingStepType GetCurrentStepType()
    {
        if (!Workflow.TryGetStep(State.CurrentStepId, out var currentStep))
        {
            return OnboardingStepType.Normal;
        }

        return currentStep.Type;
    }

    private static bool IsOwner(Roles roles)
    {
        return roles.HasRole(TenantRoles.Owner);
    }

#if TESTINGONLY
    public Result<Error> Destroy()
    {
        return RaisePermanentDeleteEvent(
            OrganizationsDomain.Events.Onboarding.Deleted(Id, OrganizationId));
    }
#endif
}