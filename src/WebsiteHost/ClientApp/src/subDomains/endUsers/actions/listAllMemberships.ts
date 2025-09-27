import useActionQuery from '../../../framework/actions/ActionQuery';
import { listMembershipsForCaller } from '../../../framework/api/apiHost1';
import endUserCacheKeys from '../../endUsers/actions/responseCache.ts';


export const ListAllMembershipsAction = () =>
  useActionQuery({
    request: (request) => listMembershipsForCaller(request),
    transform: (res) => res.memberships,
    cacheKey: endUserCacheKeys.memberships.me
  });
