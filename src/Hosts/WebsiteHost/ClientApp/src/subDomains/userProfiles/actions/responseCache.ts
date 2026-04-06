import { CacheKeys } from '../../../framework/actions/ActionCommand.ts';

const userProfileCacheKeys = {
  all: ['userProfiles'],
  me: ['userProfiles', 'me'],
  profile: {
    mutate: (userId: string) => [[...userProfileCacheKeys.all, userId]] as CacheKeys
  }
};

export default userProfileCacheKeys;
