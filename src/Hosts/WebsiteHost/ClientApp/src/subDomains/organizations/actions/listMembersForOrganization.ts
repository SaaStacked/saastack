import { useActionQuery } from '../../../framework/actions/ActionQuery';
import {
  listMembersForOrganization,
  ListMembersForOrganizationData,
  ListMembersForOrganizationResponse,
  OrganizationMember
} from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache.ts';


export enum ListMembersForOrganizationErrorCodes {
  not_owner = 'not_owner'
}

export const ListMembersForOrganizationAction = (id: string) =>
  useActionQuery<
    ListMembersForOrganizationData,
    ListMembersForOrganizationResponse,
    OrganizationMember[],
    ListMembersForOrganizationErrorCodes
  >({
    request: () => listMembersForOrganization({ path: { Id: id } }),
    transform: (res) => res.members,
    passThroughErrors: {
      403: ListMembersForOrganizationErrorCodes.not_owner
    },
    cacheKey: organizationCacheKeys.organization.members.query(id)
  });
