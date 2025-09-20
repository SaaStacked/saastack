import useActionQuery from '../../../framework/actions/ActionQuery.ts';
import { getProfileForCaller } from '../../../framework/api/apiHost1';
import userProfileCacheKeys from './responseCache.ts';

export const GetProfileForCallerAction = () =>
  useActionQuery({
    request: () => getProfileForCaller(),
    transform: (res) => res.profile,
    cacheKey: userProfileCacheKeys.me
  });
