const userProfileCacheKeys = {
  all: ['userProfiles'],
  me: ['userProfiles', 'me'],
  profile: {
    query: (userId: string) => [...userProfileCacheKeys.all, userId],
    mutate: (userId: string) => [userProfileCacheKeys.me, userProfileCacheKeys.profile.query(userId)]
  }
};

export default userProfileCacheKeys;
