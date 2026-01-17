import { useActionQuery } from '../../../framework/actions/ActionQuery';
import {
  listMembershipsForCaller,
  ListMembershipsForCallerResponse,
  Membership
} from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/EmptyRequest.ts';
import endUserCacheKeys from '../../endUsers/actions/responseCache.ts';


export const ListAllMembershipsAction = () =>
  useActionQuery<EmptyRequest, ListMembershipsForCallerResponse, Membership[]>({
    request: (request) => listMembershipsForCaller(request),
    transform: (res) => res.memberships,
    cacheKey: endUserCacheKeys.memberships.me
  });
