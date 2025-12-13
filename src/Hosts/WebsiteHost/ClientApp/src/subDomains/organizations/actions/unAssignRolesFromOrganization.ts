import { useActionCommand } from '../../../framework/actions/ActionCommand';
import {
  GetOrganizationResponse,
  unassignRolesFromOrganizationPatch,
  UnassignRolesFromOrganizationRequest
} from '../../../framework/api/apiHost1';
import { AssignRolesToOrganizationErrorCodes } from './assignRolesToOrganization';
import organizationCacheKeys from './responseCache';


export const UnAssignRolesFromOrganizationAction = (id: string) =>
  useActionCommand<UnassignRolesFromOrganizationRequest, GetOrganizationResponse, AssignRolesToOrganizationErrorCodes>({
    request: (data) =>
      unassignRolesFromOrganizationPatch({
        path: { Id: id },
        body: data
      }),
    passThroughErrors: {
      400: AssignRolesToOrganizationErrorCodes.not_member,
      403: AssignRolesToOrganizationErrorCodes.not_owner
    },
    invalidateCacheKeys: organizationCacheKeys.organization.members.mutate(id)
  });
