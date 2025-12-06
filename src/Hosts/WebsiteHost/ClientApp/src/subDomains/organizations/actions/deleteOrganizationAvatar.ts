import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { deleteOrganizationAvatar, DeleteOrganizationAvatarResponse } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/EmptyRequest.ts';
import organizationCacheKeys from './responseCache.ts';


export const DeleteOrganizationAvatarAction = (id: string) =>
  useActionCommand<EmptyRequest, DeleteOrganizationAvatarResponse>({
    request: (_request) =>
      deleteOrganizationAvatar({
        path: { Id: id }
      }),
    invalidateCacheKeys: organizationCacheKeys.organization.mutate(id)
  });
