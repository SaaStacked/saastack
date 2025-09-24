import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { deleteProfileAvatar, DeleteProfileAvatarResponse } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/apiHost1/emptyRequest.ts';
import userProfileCacheKeys from './responseCache.ts';

export const DeleteProfileAvatarAction = (userId: string) =>
  useActionCommand<EmptyRequest, DeleteProfileAvatarResponse>({
    request: (_request) =>
      deleteProfileAvatar({
        path: { UserId: userId }
      }),
    invalidateCacheKeys: [[userProfileCacheKeys.me]]
  });
