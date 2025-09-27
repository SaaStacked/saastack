const organizationCacheKeys = {
  all: ['organizations'] as const,
  organization: {
    query: (organizationId: string) => [`organizations.${organizationId}`] as const,
    mutate: (organizationId: string) => [...organizationCacheKeys.all, `organizations.${organizationId}`] as const
  }
};

export default organizationCacheKeys;
