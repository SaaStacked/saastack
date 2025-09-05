import { getProfileForCaller } from '../../api/apiHost1';
import useActionQuery from '../ActionQuery.ts';
import userProfileCacheKeys from './responseCache.ts';


export const GetProfileForCallerAction = () =>
  useActionQuery({
    request: () => getProfileForCaller(),
    transform: (res) => res.profile,
    cacheKey: userProfileCacheKeys.me
  });
