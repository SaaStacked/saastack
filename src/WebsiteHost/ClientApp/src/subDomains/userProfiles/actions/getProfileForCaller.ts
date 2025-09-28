import { useActionQuery } from '../../../framework/actions/ActionQuery.ts';
import { getProfileForCaller, GetProfileForCallerResponse, UserProfileForCaller } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/apiHost1/emptyRequest.ts';
import userProfileCacheKeys from './responseCache.ts';


export const GetProfileForCallerAction = () =>
  useActionQuery<EmptyRequest, GetProfileForCallerResponse, UserProfileForCaller>({
    request: () => getProfileForCaller(),
    transform: (res) => res.profile,
    cacheKey: userProfileCacheKeys.me
  });
