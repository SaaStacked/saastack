import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  GetOnboardingResponse,
  moveBackWorkflowStepPut,
  MoveBackWorkflowStepRequest
} from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache';


export enum OnboardingNavigationErrorCodes {
  invalid_direction = 'invalid_direction'
}

export const MoveBackWorkflowStepAction = (organizationId: string) =>
  useActionCommand<MoveBackWorkflowStepRequest, GetOnboardingResponse, OnboardingNavigationErrorCodes>({
    request: (request) =>
      moveBackWorkflowStepPut({
        path: { Id: organizationId },
        body: request
      }),
    passThroughErrors: {
      405: OnboardingNavigationErrorCodes.invalid_direction
    },
    invalidateCacheKeys: organizationCacheKeys.organization.onboarding.navigate(organizationId)
  });
