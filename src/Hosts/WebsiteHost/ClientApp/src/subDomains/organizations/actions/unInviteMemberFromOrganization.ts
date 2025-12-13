import { useActionCommand } from '../../../framework/actions/ActionCommand';
import {
  unInviteMemberFromOrganization,
  UnInviteMemberFromOrganizationResponse
} from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/EmptyRequest.ts';
import { InviteMemberToOrganizationErrorCodes } from './inviteMemberToOrganization.ts';
import organizationCacheKeys from './responseCache.ts';

export const UnInviteMemberFromOrganizationAction = (id: string, userId: string) =>
  useActionCommand<EmptyRequest, UnInviteMemberFromOrganizationResponse, InviteMemberToOrganizationErrorCodes>({
    request: () =>
      unInviteMemberFromOrganization({
        path: { Id: id, UserId: userId }
      }),
    passThroughErrors: {
      400: InviteMemberToOrganizationErrorCodes.personal_organization,
      403: InviteMemberToOrganizationErrorCodes.not_owner
    },
    invalidateCacheKeys: organizationCacheKeys.organization.members.mutate(id)
  });
