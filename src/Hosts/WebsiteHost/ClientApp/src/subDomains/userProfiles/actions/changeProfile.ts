import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { changeProfilePatch, ChangeProfileRequest, GetProfileResponse } from '../../../framework/api/apiHost1';
import userProfileCacheKeys from './responseCache.ts';


export const ChangeProfileAction = (userId: string) =>
  useActionCommand<ChangeProfileRequest, GetProfileResponse>({
    request: (request) => changeProfilePatch({ body: request, path: { UserId: userId } }),
    invalidateCacheKeys: userProfileCacheKeys.profile.mutate(userId)
  });
