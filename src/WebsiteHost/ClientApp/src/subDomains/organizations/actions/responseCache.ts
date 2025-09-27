const organizationCacheKeys = {
  all: ['organizations.all'] as const,
  organization: (organizationId: string) => [...organizationCacheKeys.all, `organizations.${organizationId}`] as const
};

export default organizationCacheKeys;
