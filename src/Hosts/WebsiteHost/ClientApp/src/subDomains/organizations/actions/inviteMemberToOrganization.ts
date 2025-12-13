import { useActionCommand } from '../../../framework/actions/ActionCommand';
import {
  inviteMemberToOrganization,
  InviteMemberToOrganizationRequest,
  InviteMemberToOrganizationResponse
} from '../../../framework/api/apiHost1';
import organizationCacheKeys from './responseCache.ts';

export enum InviteMemberToOrganizationErrorCodes {
  personal_organization = 'personal_organization',
  not_owner = 'not_owner'
}

export const InviteMemberToOrganizationAction = (id: string) =>
  useActionCommand<
    InviteMemberToOrganizationRequest,
    InviteMemberToOrganizationResponse,
    InviteMemberToOrganizationErrorCodes
  >({
    request: request =>
      inviteMemberToOrganization({
        path: { Id: id },
        body: {
          email: request.email
        }
      }),
    passThroughErrors: {
      400: InviteMemberToOrganizationErrorCodes.personal_organization,
      403: InviteMemberToOrganizationErrorCodes.not_owner
    },
    invalidateCacheKeys: organizationCacheKeys.organization.members.mutate(id)
  });
