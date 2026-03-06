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
  OrganizationOnboardingStatus,
  OrganizationOnboardingWorkflow
} from '../../../framework/api/apiHost1';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { RoutePaths } from '../../../framework/constants.ts';
import { useCurrentUser } from '../../../framework/providers/CurrentUserContext.tsx';
import { CompleteOnboardingWorkflowAction } from '../actions/completeOnboardingWorkflow.ts';
import { GetOnboardingWorkflowAction } from '../actions/getOnboardingWorkflow.ts';
import { InitiateOnboardingWorkflowAction } from '../actions/initiateOnboardingWorkflow.ts';
import { MoveBackWorkflowStepAction, OnboardingNavigationErrorCodes } from '../actions/moveBackWorkflowStep.ts';
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
          getOnboardingTrigger.current?.execute();
          break;
        case OrganizationOnboardingStatus.IN_PROGRESS:
          getOnboardingTrigger.current?.execute();
          break;
        case OrganizationOnboardingStatus.COMPLETE:
          navigate(RoutePaths.Home);
          break;
      }
    } else {
      navigate(RoutePaths.Home);
    }
  }, [organization?.onboardingStatus]);

  return (
    <FormPage title={translate('pages.organizations.onboarding.title')}>
      {shouldInitiateOnboarding(organization) && (
        <PageAction
          id="initiate_workflow"
          action={initiateAction}
          ref={initiateWorkflowTrigger}
          loadingMessage={translate('pages.organizations.onboarding.loader')}
          onSuccess={() => getOnboardingTrigger.current?.execute()}
        >
          <p>{translate('pages.organizations.onboarding.states.initiated')}</p>
        </PageAction>
      )}

      <PageAction
        id="get_onboarding"
        action={getOnboarding}
        ref={getOnboardingTrigger}
        loadingMessage={translate('pages.organizations.onboarding.loader')}
      >
        <OnboardingWorkflow workflow={getOnboarding.lastSuccessResponse!} />
      </PageAction>
    </FormPage>
  );
}

interface OnboardingWorkflowProps {
  workflow: OrganizationOnboardingWorkflow;
}

function OnboardingWorkflow({ workflow }: OnboardingWorkflowProps) {
  const { t: translate } = useTranslation();
  const [currentWorkflow, setCurrentWorkflow] = useState(workflow);
  const currentStep = currentWorkflow.state?.currentStep!;
  const currentStepId = currentStep.id;

  return (
    <div className="w-full max-w-4xl mx-auto" data-testid="onboarding_workflow">
      <ProgressBar workflow={currentWorkflow} />

      {/* Current step content */}
      <div
        className="bg-white dark:bg-neutral-800 rounded-lg shadow-md p-8 mb-6"
        key={currentStepId}
        data-testid={`onboarding_step_${currentStepId}`}
      >
        <h2 className="text-2xl font-bold mb-4 text-neutral-900 dark:text-neutral-100">
          {translate(`pages.organizations.onboarding.steps.${currentStep.id}.title`)}
        </h2>
        <p className="text-neutral-700 dark:text-neutral-300 mb-6">
          {translate(`pages.organizations.onboarding.steps.${currentStep.id}.description`)}
        </p>
      </div>

      <NavigationButtons workflow={currentWorkflow} setCurrentWorkflow={setCurrentWorkflow} />
    </div>
  );
}

interface NavigationButtonsProps {
  workflow: OrganizationOnboardingWorkflow;
  setCurrentWorkflow: (workflow: OrganizationOnboardingWorkflow) => void;
}

function NavigationButtons({ workflow, setCurrentWorkflow }: NavigationButtonsProps) {
  const { t: translate } = useTranslation();
  const navigate = useNavigate();
  const { refetch: refetchCurrentUser } = useCurrentUser();

  const moveForward = MoveForwardWorkflowStepAction(workflow.organizationId);
  const moveBack = MoveBackWorkflowStepAction(workflow.organizationId);
  const complete = CompleteOnboardingWorkflowAction(workflow.organizationId);

  const currentStep = workflow.state?.currentStep;
  const isFirstStep = currentStep?.id === workflow.workflow.startStepId;
  const isLastStep = currentStep?.id === workflow.workflow.endStepId;

  return (
    <div className="flex justify-between items-center" id="onboarding_navigation">
      <div>
        {!isFirstStep && (
          <ButtonAction
            id="onboarding_back"
            action={moveBack}
            requestData={{}}
            expectedErrorMessages={{
              [OnboardingNavigationErrorCodes.invalid_direction]: translate(
                'pages.organizations.onboarding.errors.invalid_direction'
              )
            }}
            onSuccess={(params: { requestData?: MoveBackWorkflowStepRequest; response: GetOnboardingResponse }) =>
              setCurrentWorkflow(params.response.workflow)
            }
            variant="outline"
            label={translate('pages.organizations.onboarding.labels.back')}
          />
        )}
      </div>
      <div className="flex gap-3">
        {isLastStep ? (
          <ButtonAction
            id="onboarding_complete"
            action={complete}
            requestData={{}}
            onSuccess={(params: {
              requestData?: CompleteOnboardingWorkflowRequest;
              response: GetOnboardingResponse;
            }) => {
              setCurrentWorkflow(params.response.workflow);
              refetchCurrentUser();
              navigate(RoutePaths.Home);
            }}
            variant="brand-primary"
            label={translate('pages.organizations.onboarding.labels.finish')}
          />
        ) : (
          <ButtonAction
            id="onboarding_next"
            action={moveForward}
            requestData={{}}
            expectedErrorMessages={{
              [OnboardingNavigationErrorCodes.invalid_direction]: translate(
                'pages.organizations.onboarding.errors.invalid_direction'
              )
            }}
            onSuccess={(params: { requestData?: MoveForwardWorkflowStepRequest; response: GetOnboardingResponse }) =>
              setCurrentWorkflow(params.response.workflow)
            }
            variant="brand-primary"
            label={translate('pages.organizations.onboarding.labels.next')}
          />
        )}
      </div>
    </div>
  );
}

export function ProgressBar({ workflow }: OnboardingWorkflowProps) {
  const { t: translate } = useTranslation();
  const pathTaken = workflow.state?.pathTaken || [];
  const pathAhead = workflow.state?.pathAhead || [];
  const currentStepId = workflow.state?.currentStep.id;
  const currentStepIndex = pathTaken.length;
  const allSteps = [...pathTaken, workflow.state?.currentStep!, ...pathAhead].map(step => ({
    id: step.id,
    title: step.title,
    description: step.title,
    weight: step.weight
  }));

  const progressPercentage = workflow.state?.progressPercentage || 0;

  // Calculate step positions based on weight
  const totalWeight = allSteps.reduce((sum, step) => sum + step.weight, 0);
  let cumulativeWeight = 0;
  const stepsWithPositions = allSteps.map((step) => {
    const position = totalWeight > 0 ? (cumulativeWeight / totalWeight) * 100 : 0;
    cumulativeWeight += step.weight;
    return { ...step, position };
  });

  return (
    <div className="mb-6" id="onboarding_progress">
      <div
        className="relative"
        style={{ paddingLeft: '12px', paddingRight: '12px', minHeight: '80px', paddingBottom: '40px' }}
      >
        {/* Progress line container */}
        <div className="absolute top-2 left-3 right-3 flex items-center" style={{ zIndex: 0 }}>
          <div className="flex-1 relative">
            <div className="h-2 border-2 border-neutral-100 dark:border-neutral-600 bg-neutral-200 dark:bg-neutral-800"></div>
            <div
              className="absolute top-0.5 left-0 h-1 bg-success-600 border-neutral-400 dark:border-neutral-900 transition-all duration-500"
              style={{ width: `${progressPercentage}%` }}
            ></div>
          </div>
        </div>

        {/* Step circles positioned by weight */}
        {stepsWithPositions.map((step, index) => {
          const isCompleted = index < currentStepIndex;
          const isCurrent = step.id === currentStepId;

          return (
            <div
              key={step.id}
              className="absolute flex flex-col items-center"
              style={{
                left: `${step.position}%`,
                top: 0,
                transform: 'translateX(-50%)',
                zIndex: 1
              }}
            >
              {/* Circle */}
              <div
                className={`w-6 h-6 rounded-full flex items-center justify-center transition-all duration-300 border-2 ${
                  isCompleted || isCurrent
                    ? 'bg-success-600 text-white border-neutral-100 dark:border-neutral-600'
                    : 'bg-neutral-100 dark:bg-neutral-800 border-neutral-300 dark:border-neutral-600 text-neutral-400'
                } ${isCurrent ? 'ring-4 ring-success-300 dark:ring-success-900' : ''}`}
              >
                {isCompleted ? (
                  <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                    <path
                      fillRule="evenodd"
                      d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                      clipRule="evenodd"
                    />
                  </svg>
                ) : (
                  <span className="text-sm font-semibold">{index + 1}</span>
                )}
              </div>

              {/* Label */}
              <div className="mt-3 text-center max-w-[120px]" data-testid={`onboarding_step_${step.id}_label`}>
                <p
                  className={`text-xs font-medium ${
                    isCurrent
                      ? 'text-success-600 dark:text-success-400'
                      : isCompleted
                        ? 'text-neutral-700 dark:text-neutral-300'
                        : 'text-neutral-400 dark:text-neutral-500'
                  }`}
                >
                  {translate(`pages.organizations.onboarding.steps.${step.id}.title`)}
                </p>
              </div>
            </div>
          );
        })}
      </div>

      {/* Progress percentage */}
      <p className="text-xs text-neutral-600 dark:text-neutral-400 text-center">
        {Math.round(progressPercentage)}% {translate('pages.organizations.onboarding.labels.progress_complete')}
      </p>
    </div>
  );
}
