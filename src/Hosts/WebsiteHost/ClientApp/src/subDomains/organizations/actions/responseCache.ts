const organizationCacheKeys = {
  all: ['organizations'] as const,
  organization: {
    query: (organizationId: string) => [`organizations.${organizationId}`] as const,
    mutate: (organizationId: string) => [...organizationCacheKeys.all, `organizations.${organizationId}`] as const,
    members: {
      query: (organizationId: string) => [`organizations.${organizationId}.members`] as const,
      mutate: (organizationId: string) => [`organizations.${organizationId}.members`] as const
    }
  }
};

export default organizationCacheKeys;
