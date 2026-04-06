const organizationCacheKeys = {
  import { CacheKeys } from '../../../framework/actions/ActionCommand.ts';
  import userProfileCacheKeys from '../../userProfiles/actions/responseCache.ts';

  const organizationCacheKeys = {
    all: ['organizations'],
    organization: {
      query: (organizationId: string) => [...organizationCacheKeys.all, organizationId],
      mutate: (organizationId: string) => [organizationCacheKeys.organization.query(organizationId)] as CacheKeys,
      switch: (organizationId: string) =>
        [userProfileCacheKeys.me, organizationCacheKeys.organization.query(organizationId)] as CacheKeys,
      members: {
        all: ['organizations', 'members'],
        query: (organizationId: string) => [...organizationCacheKeys.organization.members.all, organizationId],
        mutate: (organizationId: string) =>
          [organizationCacheKeys.organization.members.query(organizationId)] as CacheKeys
      },
    }
  };

  export default organizationCacheKeys;
