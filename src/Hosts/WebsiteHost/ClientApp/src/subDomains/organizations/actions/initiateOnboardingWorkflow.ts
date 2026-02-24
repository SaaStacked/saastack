import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  GetOnboardingResponse,
  initiateOnboardingWorkflow,
  InitiateOnboardingWorkflowRequest
} from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache';

export const InitiateOnboardingWorkflowAction = (organizationId: string) =>
  useActionCommand<InitiateOnboardingWorkflowRequest, GetOnboardingResponse>({
    request: (request) =>
      initiateOnboardingWorkflow({
        path: { Id: organizationId },
        body: request
      }),
    invalidateCacheKeys: organizationCacheKeys.organization.onboarding.mutate(organizationId)
  });
