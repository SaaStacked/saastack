import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  changeOrganizationPatch,
  ChangeOrganizationPatchResponse,
  ChangeOrganizationRequest
} from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache.ts';

export const ChangeDefaultOrganizationAction = (id: string) =>
  useActionCommand<ChangeOrganizationRequest, ChangeOrganizationPatchResponse>({
    request: (request) =>
      changeOrganizationPatch({
        body: {
          ...request
        },
        path: {
          Id: id
        }
      }),
    invalidateCacheKeys: organizationCacheKeys.organization.switch(id)
  });
