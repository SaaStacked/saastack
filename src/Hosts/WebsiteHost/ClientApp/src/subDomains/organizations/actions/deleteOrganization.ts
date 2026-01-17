import { useActionCommand } from '../../../framework/actions/ActionCommand';
import { deleteOrganization, DeleteOrganizationResponse } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/EmptyRequest.ts';
import organizationCacheKeys from './responseCache';

export const DeleteOrganizationAction = (id: string) =>
  useActionCommand<EmptyRequest, DeleteOrganizationResponse>({
    request: (_request) => deleteOrganization({ path: { Id: id } }),
    invalidateCacheKeys: organizationCacheKeys.organization.mutate(id)
  });
