import useActionQuery from '../../../framework/actions/ActionQuery';
import { getOrganization } from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache';


export const GetOrganizationAction = (id: string) =>
  useActionQuery({
    request: (request) =>
      getOrganization({
        ...request,
        path: {
          Id: id
        }
      }),
    transform: (res) => res.organization,
    cacheKey: organizationCacheKeys.organization.query(id)
  });
