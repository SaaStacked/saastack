const ancillaryCacheKeys = {
  all: ['ancillary.features.all'] as const,
  features: {
    all: ['ancillary.features.all'] as const,
    feature: (name: string) => [...ancillaryCacheKeys.features.all, `ancillary.features.feature.${name}`] as const
  }
};

export default ancillaryCacheKeys;
