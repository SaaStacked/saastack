import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { completeOnboardingWorkflowPut, CompleteOnboardingWorkflowRequest, GetOnboardingResponse } from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache';


export const CompleteOnboardingWorkflowAction = (organizationId: string) =>
  useActionCommand<CompleteOnboardingWorkflowRequest, GetOnboardingResponse>({
    request: (request) =>
      completeOnboardingWorkflowPut({
        path: { Id: organizationId },
        body: request
      }),
    invalidateCacheKeys: organizationCacheKeys.organization.onboarding.complete(organizationId)
  });
