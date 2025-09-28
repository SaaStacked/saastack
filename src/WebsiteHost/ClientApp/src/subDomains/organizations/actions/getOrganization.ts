import { useActionQuery } from '../../../framework/actions/ActionQuery';
import { getOrganization, GetOrganizationResponse, Organization } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/apiHost1/emptyRequest.ts';
import organizationCacheKeys from './responseCache';


export const GetOrganizationAction = (id: string) =>
  useActionQuery<EmptyRequest, GetOrganizationResponse, Organization>({
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
