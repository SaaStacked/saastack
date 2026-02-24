import { useActionQuery } from '../../../framework/actions/ActionQuery';
import {
  GetOnboardingResponse,
  getOnboardingWorkflow,
  GetOnboardingWorkflowData,
  OrganizationOnboardingWorkflow
} from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache';

export const GetOnboardingWorkflowAction = (organizationId: string) =>
  useActionQuery<GetOnboardingWorkflowData, GetOnboardingResponse, OrganizationOnboardingWorkflow>({
    request: () =>
      getOnboardingWorkflow({
        path: { Id: organizationId }
      }),
    transform: (res) => res.workflow,
    cacheKey: organizationCacheKeys.organization.onboarding.query(organizationId)
  });
