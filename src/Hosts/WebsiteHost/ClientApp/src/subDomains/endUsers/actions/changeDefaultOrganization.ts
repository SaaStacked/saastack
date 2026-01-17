import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  changeDefaultOrganizationPatch,
  ChangeDefaultOrganizationPatchResponse,
  ChangeDefaultOrganizationRequest
} from '../../../framework/api/apiHost1';
import endUserCacheKeys from './responseCache';


export const ChangeDefaultOrganizationAction = () =>
  useActionCommand<ChangeDefaultOrganizationRequest, ChangeDefaultOrganizationPatchResponse>({
    request: (request) =>
      changeDefaultOrganizationPatch({
        body: {
          ...request
        }
      }),
    invalidateCacheKeys: endUserCacheKeys.users.me
  });
