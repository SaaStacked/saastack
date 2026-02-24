import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import {
  CompleteOnboardingWorkflowRequest,
  GetOnboardingResponse,
  GetOnboardingWorkflowData,
  InitiateOnboardingWorkflowRequest,
  MoveBackWorkflowStepRequest,
  MoveForwardWorkflowStepRequest,
  OrganizationOnboardingStatus
} from '../../../framework/api/apiHost1';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { RoutePaths } from '../../../framework/constants.ts';
import { useCurrentUser } from '../../../framework/providers/CurrentUserContext.tsx';
import { CompleteOnboardingWorkflowAction } from '../actions/completeOnboardingWorkflow.ts';
import { GetOnboardingWorkflowAction } from '../actions/getOnboardingWorkflow.ts';
import { InitiateOnboardingWorkflowAction } from '../actions/initiateOnboardingWorkflow.ts';
import { MoveBackWorkflowStepAction } from '../actions/moveBackWorkflowStep.ts';
import { MoveForwardWorkflowStepAction } from '../actions/moveForwardWorkflowStep.ts';
import { customWorkflow, shouldBeOnboarding, shouldInitiateOnboarding } from './Onboarding.ts';

export function OrganizationOnboardingPage() {
  const { t: translate } = useTranslation();
  const { organization } = useCurrentUser();
  const organizationId = organization?.id || '';
  const navigate = useNavigate();

  const getOnboarding = GetOnboardingWorkflowAction(organizationId);
  const getOnboardingTrigger = useRef<PageActionRef<GetOnboardingWorkflowData>>(null);
  const initiateAction = InitiateOnboardingWorkflowAction(organizationId);
  const initiateWorkflowTrigger = useRef<PageActionRef<InitiateOnboardingWorkflowRequest>>(null);

  useEffect(() => {
    if (shouldBeOnboarding(organization)) {
      switch (organization!.onboardingStatus) {
        case OrganizationOnboardingStatus.NOT_STARTED:
          initiateWorkflowTrigger.current?.execute({ workflow: customWorkflow() });
          break;
        case OrganizationOnboardingStatus.IN_PROGRESS:
          getOnboardingTrigger.current?.execute();
          break;
        case OrganizationOnboardingStatus.COMPLETE:
          navigate(RoutePaths.Home);
          break;
      }
    }
  }, [organization?.onboardingStatus]);

  return (
    <FormPage title={translate('pages.organizations.onboarding.title')}>
      {shouldInitiateOnboarding(organization) ? (
        <PageAction
          id="initiate_workflow"
          action={initiateAction}
          ref={initiateWorkflowTrigger}
          loadingMessage={translate('pages.organizations.onboarding.loader')}
          onSuccess={() =>
            getOnboardingTrigger.current?.execute({ path: { Id: organizationId! } } as GetOnboardingWorkflowData)
          }
        >
          <p>{translate('pages.organizations.onboarding.states.initiated')}</p>
        </PageAction>
      ) : (
        <PageAction
          id="get_onboarding"
          action={getOnboarding}
          ref={getOnboardingTrigger}
          loadingMessage={translate('pages.organizations.onboarding.loader')}
        >
          <OnboardingWorkflow workflow={getOnboarding.lastSuccessResponse} />
        </PageAction>
      )}
    </FormPage>
  );
}

interface OnboardingWorkflowContentProps {
  workflow: any;
}

function OnboardingWorkflow({ workflow }: OnboardingWorkflowContentProps) {
  const { t: translate } = useTranslation();
  const navigate = useNavigate();
  const [currentWorkflow, setCurrentWorkflow] = useState(workflow);

  const moveForward = MoveForwardWorkflowStepAction(currentWorkflow.organizationId);
  const moveBack = MoveBackWorkflowStepAction(currentWorkflow.organizationId);
  const complete = CompleteOnboardingWorkflowAction(currentWorkflow.organizationId);

  const currentStep = currentWorkflow.state?.currentStep;
  const isFirstStep = currentStep?.id === currentWorkflow.workflow.startStepId;
  const isLastStep = currentStep?.id === currentWorkflow.workflow.endStepId;
  const progressPercentage = currentWorkflow.state?.progressPercentage || 0;

  return (
    <div className="w-full max-w-2xl mx-auto">
      {/* Progress bar */}
      <div className="mb-8">
        <div className="w-full bg-neutral-200 dark:bg-neutral-700 rounded-full h-2.5">
          <div
            className="bg-brand-primary-600 h-2.5 rounded-full transition-all duration-300"
            style={{ width: `${progressPercentage}%` }}
          ></div>
        </div>
        <p className="text-sm text-neutral-600 dark:text-neutral-400 mt-2 text-center">
          {Math.round(progressPercentage)}% {translate('pages.organizations.onboarding.labels.complete')}
        </p>
      </div>

      {/* Current step content */}
      <div className="bg-white dark:bg-neutral-800 rounded-lg shadow-md p-8 mb-6">
        <h2 className="text-2xl font-bold mb-4 text-neutral-900 dark:text-neutral-100">{currentStep?.title}</h2>
        <p className="text-neutral-700 dark:text-neutral-300 mb-6">
          {currentWorkflow.workflow.steps[currentStep?.id || '']?.description}
        </p>
      </div>

      {/* Navigation buttons */}
      <div className="flex justify-between items-center">
        <div>
          {!isFirstStep && (
            <ButtonAction
              id="onboarding_back"
              action={moveBack}
              requestData={{}}
              variant="outline"
              label={translate('pages.organizations.onboarding.labels.back')}
              onSuccess={(params: { requestData?: MoveBackWorkflowStepRequest; response: GetOnboardingResponse }) =>
                setCurrentWorkflow(params.response.workflow)
              }
            />
          )}
        </div>
        <div className="flex gap-3">
          {isLastStep ? (
            <ButtonAction
              id="onboarding_finish"
              action={complete}
              requestData={{}}
              variant="brand-primary"
              label={translate('pages.organizations.onboarding.labels.finish')}
              onSuccess={(params: {
                requestData?: CompleteOnboardingWorkflowRequest;
                response: GetOnboardingResponse;
              }) => {
                setCurrentWorkflow(params.response.workflow);
                navigate(RoutePaths.Home);
              }}
            />
          ) : (
            <ButtonAction
              id="onboarding_next"
              action={moveForward}
              requestData={{}}
              variant="brand-primary"
              label={translate('pages.organizations.onboarding.labels.next')}
              onSuccess={(params: { requestData?: MoveForwardWorkflowStepRequest; response: GetOnboardingResponse }) =>
                setCurrentWorkflow(params.response.workflow)
              }
            />
          )}
        </div>
      </div>
    </div>
  );
}
