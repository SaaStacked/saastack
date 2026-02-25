import userProfileCacheKeys from '../../userProfiles/actions/responseCache.ts';


const organizationCacheKeys = {
  all: ['organizations'] as const,
  organization: {
    query: (organizationId: string) => [...organizationCacheKeys.all, organizationId] as const,
    mutate: (organizationId: string) => [...organizationCacheKeys.all, organizationId] as const,
    switch: (organizationId: string) =>
      [...userProfileCacheKeys.me, ...organizationCacheKeys.all, organizationId] as const,
    members: {
      all: ['organizations', 'members'] as const,
      query: (organizationId: string) => [...organizationCacheKeys.organization.members.all, organizationId] as const,
      mutate: (organizationId: string) => [...organizationCacheKeys.organization.members.all, organizationId] as const
    },
    onboarding: {
      all: ['organizations', 'onboarding'] as const,
      query: (organizationId: string) =>
        [...organizationCacheKeys.organization.onboarding.all, organizationId] as const,
      navigate: (organizationId: string) =>
        [...organizationCacheKeys.organization.onboarding.all, organizationId] as const,
      complete: (organizationId: string) =>
        [
          ...userProfileCacheKeys.me,
          ...organizationCacheKeys.organization.mutate(organizationId),
          ...organizationCacheKeys.organization.onboarding.navigate(organizationId)
        ] as const
    }
  }
};

export default organizationCacheKeys;
