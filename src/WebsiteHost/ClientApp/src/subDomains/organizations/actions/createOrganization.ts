import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  createOrganization,
  CreateOrganizationRequest,
  CreateOrganizationResponse
} from '../../../framework/api/apiHost1';
import endUserCacheKeys from '../../identity/endUsers/responseCache.ts';

export const CreateOrganizationAction = () =>
  useActionCommand<CreateOrganizationRequest, CreateOrganizationResponse>({
    request: (request) => createOrganization({ body: request }),
    invalidateCacheKeys: endUserCacheKeys.memberships.all
  });
