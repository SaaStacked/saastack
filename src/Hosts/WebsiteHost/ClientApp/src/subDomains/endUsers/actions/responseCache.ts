import { TenantedCacheKey } from '../../../framework/constants.ts';
import organizationCacheKeys from '../../organizations/actions/responseCache.ts';
import userProfileCacheKeys from '../../userProfiles/actions/responseCache.ts';

const endUserCacheKeys = {
  all: ['users'],
  switchOrganization: (organizationId: string) => [
    [TenantedCacheKey], // clears all tenanted resources
    endUserCacheKeys.users.me,
    endUserCacheKeys.memberships.me,
    userProfileCacheKeys.me,
    organizationCacheKeys.organization.query(organizationId)
  ],
  memberships: {
    all: ['users', 'memberships'],
    me: ['users', 'memberships', 'me'],
    query: (userId: string) => [...endUserCacheKeys.memberships.all, userId],
    mutate: (userId: string) => [endUserCacheKeys.memberships.query(userId)]
  },
  users: {
    all: ['users'],
    me: ['users', 'me']
  }
};

export default endUserCacheKeys;
