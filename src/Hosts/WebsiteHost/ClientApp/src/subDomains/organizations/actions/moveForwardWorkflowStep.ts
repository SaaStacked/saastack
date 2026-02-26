import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  GetOnboardingResponse,
  moveForwardWorkflowStepPut,
  MoveForwardWorkflowStepRequest
} from '../../../framework/api/apiHost1';
import { OnboardingNavigationErrorCodes } from './moveBackWorkflowStep.ts';
import organizationCacheKeys from './responseCache';


export const MoveForwardWorkflowStepAction = (organizationId: string) =>
  useActionCommand<MoveForwardWorkflowStepRequest, GetOnboardingResponse>({
    request: (request) =>
      moveForwardWorkflowStepPut({
        path: { Id: organizationId },
        body: request
      }),
    passThroughErrors: {
      405: OnboardingNavigationErrorCodes.invalid_direction
    },
    invalidateCacheKeys: organizationCacheKeys.organization.onboarding.navigate(organizationId)
  });
