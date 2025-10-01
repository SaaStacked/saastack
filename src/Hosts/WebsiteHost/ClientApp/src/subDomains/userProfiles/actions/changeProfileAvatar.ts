import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { changeProfileAvatarPatch, ChangeProfileAvatarResponse } from '../../../framework/api/apiHost1';
import userProfileCacheKeys from './responseCache.ts';


export interface ChangeProfileAvatarRequest {
  file: File;
}

export enum UploadAvatarErrors {
  invalid_image = 'invalid_image'
}

export const ChangeProfileAvatarAction = (userId: string) =>
  useActionCommand<ChangeProfileAvatarRequest, ChangeProfileAvatarResponse, UploadAvatarErrors>({
    request: (request) =>
      changeProfileAvatarPatch({
        body: {
          files: [request.file]
        },
        path: { UserId: userId },
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      }),
    passThroughErrors: {
      400: UploadAvatarErrors.invalid_image
    },
    invalidateCacheKeys: userProfileCacheKeys.profile.mutate(userId)
  });
