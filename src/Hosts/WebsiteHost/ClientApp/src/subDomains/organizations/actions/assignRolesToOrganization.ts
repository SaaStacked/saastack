import { useActionCommand } from '../../../framework/actions/ActionCommand';
import {
  assignRolesToOrganizationPatch,
  AssignRolesToOrganizationRequest,
  GetOrganizationResponse
} from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache.ts';

export enum AssignRolesToOrganizationErrorCodes {
  not_member = 'not_member',
  not_owner = 'not_owner'
}

export const AssignRolesToOrganizationAction = (id: string) =>
  useActionCommand<AssignRolesToOrganizationRequest, GetOrganizationResponse, AssignRolesToOrganizationErrorCodes>({
    request: (data) =>
      assignRolesToOrganizationPatch({
        path: { Id: id },
        body: data
      }),
    passThroughErrors: {
      400: AssignRolesToOrganizationErrorCodes.not_member,
      403: AssignRolesToOrganizationErrorCodes.not_owner
    },
    invalidateCacheKeys: organizationCacheKeys.organization.members.mutate(id)
  });
