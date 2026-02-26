import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  GetOnboardingResponse,
  initiateOnboardingWorkflow,
  InitiateOnboardingWorkflowRequest
} from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache';

export enum OnboardingInitiationErrorCodes {
  already_initiated = 'already_initiated'
}

export const InitiateOnboardingWorkflowAction = (organizationId: string) =>
  useActionCommand<InitiateOnboardingWorkflowRequest, GetOnboardingResponse, OnboardingInitiationErrorCodes>({
    request: (request) =>
      initiateOnboardingWorkflow({
        path: { Id: organizationId },
        body: request
      }),
    passThroughErrors: {
      409: OnboardingInitiationErrorCodes.already_initiated
    },
    invalidateCacheKeys: organizationCacheKeys.organization.onboarding.navigate(organizationId)
  });
