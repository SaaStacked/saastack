using Application.Resources.Shared;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Organizations.Onboarding;
using OrganizationsApplication;

namespace OrganizationsInfrastructure.Api.Onboarding;

public class OnboardingApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IOnboardingApplication _onboardingApplication;

    public OnboardingApi(ICallerContextFactory callerFactory, IOnboardingApplication onboardingApplication)
    {
        _callerFactory = callerFactory;
        _onboardingApplication = onboardingApplication;
    }

    public async Task<ApiPutPatchResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>> CompleteOnboarding(
        CompleteOnboardingWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _onboardingApplication.CompleteOnboardingAsync(_callerFactory.Create(),
            request.Id!, cancellationToken);

        return () => workflow.HandleApplicationResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>(wf =>
            new GetOnboardingResponse { Workflow = wf });
    }

    public async Task<ApiGetResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>> GetOnboarding(
        GetOnboardingWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _onboardingApplication.GetOnboardingAsync(_callerFactory.Create(),
            request.Id!, cancellationToken);

        return () => workflow.HandleApplicationResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>(wf =>
            new GetOnboardingResponse { Workflow = wf });
    }

    public async Task<ApiPutPatchResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>> MoveBackward(
        MoveBackWorkflowStepRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _onboardingApplication.MoveBackwardAsync(_callerFactory.Create(),
            request.Id!, cancellationToken);

        return () => workflow.HandleApplicationResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>(wf =>
            new GetOnboardingResponse { Workflow = wf });
    }

    public async Task<ApiPutPatchResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>> MoveForward(
        MoveForwardWorkflowStepRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _onboardingApplication.MoveForwardAsync(_callerFactory.Create(),
            request.Id!, request.NextStepId, request.Values, cancellationToken);

        return () => workflow.HandleApplicationResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>(wf =>
            new GetOnboardingResponse { Workflow = wf });
    }

    public async Task<ApiPostResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>> InitiateOnboarding(
        InitiateOnboardingWorkflowRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _onboardingApplication.InitiateOnboardingAsync(_callerFactory.Create(),
            request.Id!, request.Workflow!, cancellationToken);

        return () => workflow.HandleApplicationResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>(wf =>
            new PostResult<GetOnboardingResponse>(new GetOnboardingResponse { Workflow = wf }));
    }

    public async Task<ApiPutPatchResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>> UpdateCurrentStep(
        UpdateCurrentWorkflowStepRequest request, CancellationToken cancellationToken)
    {
        var workflow = await _onboardingApplication.UpdateCurrentStepAsync(_callerFactory.Create(),
            request.Id!, request.Values!, cancellationToken);

        return () => workflow.HandleApplicationResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>(wf =>
            new GetOnboardingResponse { Workflow = wf });
    }

#if TESTINGONLY
    public async Task<ApiPutPatchResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>> ResetWorkflow(
        ResetCurrentWorkflowRequest request, CancellationToken cancellationToken)
    {
        var organizationOnboardingWorkflow = await _onboardingApplication.ResetWorkflowAsync(_callerFactory.Create(),
            request.Id!,
            cancellationToken);

        return () =>
            organizationOnboardingWorkflow
                .HandleApplicationResult<OrganizationOnboardingWorkflow, GetOnboardingResponse>(wf =>
                    new GetOnboardingResponse { Workflow = wf });
    }
#endif
}