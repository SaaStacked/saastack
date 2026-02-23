import { useActionQuery } from '../../../framework/actions/ActionQuery';
import {
  GetOnboardingResponse,
  getOnboardingWorkflow,
  GetOnboardingWorkflowData,
  OrganizationOnboardingWorkflow
} from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache';

export enum GetOnboardingErrorCodes {
  not_initiated = 'not_initiated'
}

export const GetOnboardingWorkflowAction = (organizationId: string) =>
  useActionQuery<
    GetOnboardingWorkflowData,
    GetOnboardingResponse,
    OrganizationOnboardingWorkflow,
    GetOnboardingErrorCodes
  >({
    request: () =>
      getOnboardingWorkflow({
        path: { Id: organizationId }
      }),
    transform: (res) => res.workflow,
    passThroughErrors: {
      404: GetOnboardingErrorCodes.not_initiated
    },
    cacheKey: organizationCacheKeys.organization.onboarding.query(organizationId)
  });
