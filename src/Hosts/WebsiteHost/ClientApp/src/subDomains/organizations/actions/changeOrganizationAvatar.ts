import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { changeOrganizationAvatarPatch, ChangeOrganizationAvatarPatchResponse } from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache.ts';

export interface ChangeOrganizationAvatarRequest {
  file: File;
}

export enum UploadAvatarErrors {
  invalid_image = 'invalid_image'
}

export const ChangeOrganizationAvatarAction = (id: string) =>
  useActionCommand<ChangeOrganizationAvatarRequest, ChangeOrganizationAvatarPatchResponse, UploadAvatarErrors>({
    request: (request) =>
      changeOrganizationAvatarPatch({
        body: {
          files: [request.file]
        },
        path: { Id: id }
      }),
    passThroughErrors: {
      400: UploadAvatarErrors.invalid_image
    },
    invalidateCacheKeys: organizationCacheKeys.organization.mutate(id)
  });
