import { useActionQuery } from '../../../framework/actions/ActionQuery';
import { getOrganization, GetOrganizationResponse, Organization } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/EmptyRequest.ts';
import organizationCacheKeys from './responseCache';


export enum OrganizationErrorCodes {
  forbidden = 'forbidden'
}

export const GetOrganizationAction = (id: string) =>
  useActionQuery<EmptyRequest, GetOrganizationResponse, Organization, OrganizationErrorCodes>({
    request: (request) =>
      getOrganization({
        ...request,
        path: {
          Id: id
        }
      }),
    transform: (res) => res.organization,
    passThroughErrors: {
      403: OrganizationErrorCodes.forbidden
    },
    cacheKey: organizationCacheKeys.organization.query(id)
  });
