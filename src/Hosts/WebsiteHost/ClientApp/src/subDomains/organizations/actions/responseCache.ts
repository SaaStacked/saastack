import { TenantedCacheKey } from '../../../framework/constants.ts';
import userProfileCacheKeys from '../../userProfiles/actions/responseCache.ts';

const organizationCacheKeys = {
  all: ['organizations'],
  organization: {
    query: (organizationId: string) => [...organizationCacheKeys.all, organizationId],
    mutate: (organizationId: string) => [organizationCacheKeys.organization.query(organizationId)],
    members: {
      all: [TenantedCacheKey, 'organizations', 'members'],
      query: (organizationId: string) => [...organizationCacheKeys.organization.members.all, organizationId],
      mutate: (organizationId: string) => [organizationCacheKeys.organization.members.query(organizationId)]
    },
    onboarding: {
      all: [TenantedCacheKey, 'organizations', 'onboarding'],
      query: (organizationId: string) => [...organizationCacheKeys.organization.onboarding.all, organizationId],
      navigate: (organizationId: string) => [organizationCacheKeys.organization.onboarding.query(organizationId)],
      complete: (organizationId: string) => [
        userProfileCacheKeys.me,
        organizationCacheKeys.organization.query(organizationId),
        organizationCacheKeys.organization.onboarding.navigate(organizationId)
      ]
    }
  }
};

export default organizationCacheKeys;
