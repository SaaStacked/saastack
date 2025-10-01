const ancillaryCacheKeys = {
  all: ['ancillary'] as const,
  features: {
    all: ['ancillary.features'] as const,
    query: (name: string) => [`ancillary.features.${name}`] as const
  }
};

export default ancillaryCacheKeys;
