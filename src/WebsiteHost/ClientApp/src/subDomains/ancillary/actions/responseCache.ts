const featureFlagCacheKeys = {
  all: ['ancillary.features.all'] as const,
  flag: (flagName: string) => [...featureFlagCacheKeys.all, `ancillary.features.feature.${flagName}`] as const
};

export default featureFlagCacheKeys;
