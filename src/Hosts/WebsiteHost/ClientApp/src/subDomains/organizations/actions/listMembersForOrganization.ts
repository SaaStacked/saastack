import { useActionQuery } from '../../../framework/actions/ActionQuery';
import {
  listMembersForOrganization,
  ListMembersForOrganizationResponse,
  OrganizationMember
} from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/EmptyRequest.ts';
import organizationCacheKeys from './responseCache.ts';

export enum ListMembersForOrganizationErrorCodes {
  not_member = 'not_member'
}

export const ListMembersForOrganizationAction = (id: string) =>
  useActionQuery<
    EmptyRequest,
    ListMembersForOrganizationResponse,
    OrganizationMember[],
    ListMembersForOrganizationErrorCodes
  >({
    request: () => listMembersForOrganization({ path: { Id: id } }),
    transform: (res) => res.members,
    passThroughErrors: {
      403: ListMembersForOrganizationErrorCodes.not_member
    },
    cacheKey: organizationCacheKeys.organization.members.query(id)
  });
