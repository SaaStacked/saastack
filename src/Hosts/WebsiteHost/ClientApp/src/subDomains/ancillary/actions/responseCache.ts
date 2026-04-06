const ancillaryCacheKeys = {
  all: ['ancillary'],
  features: {
    all: ['ancillary', 'features'],
    query: (name: string) => [...ancillaryCacheKeys.features.all, name]
  }
};

export default ancillaryCacheKeys;
