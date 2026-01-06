const oAuth2CacheKeys = {
  all: ['clients'] as const,
  client: {
    query: (clientId: string) => [`clients.${clientId}`] as const,
    mutate: (clientId: string) => [...oAuth2CacheKeys.all, `clients.${clientId}`] as const,
    consent: {
      query: (clientId: string) => [`clients.${clientId}.consent`] as const,
      mutate: (clientId: string) => [`clients.${clientId}.consent`] as const
    }
  }
};

export default oAuth2CacheKeys;
