import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  createOrganization,
  CreateOrganizationRequest,
  CreateOrganizationResponse
} from '../../../framework/api/apiHost1';
import endUserCacheKeys from '../../endUsers/actions/responseCache.ts';
import organizationCacheKeys from './responseCache.ts';

export enum CreateOrganizationErrors {
  invalid_domain = 'invalid_domain',
  duplicate_domain = 'duplicate_domain'
}

export const CreateOrganizationAction = () =>
  useActionCommand<CreateOrganizationRequest, CreateOrganizationResponse, CreateOrganizationErrors>({
    request: (request) => createOrganization({ body: request }),
    passThroughErrors: {
      405: CreateOrganizationErrors.invalid_domain,
      409: CreateOrganizationErrors.duplicate_domain
    },
    invalidateCacheKeys: [endUserCacheKeys.memberships.all, organizationCacheKeys.all]
  });
