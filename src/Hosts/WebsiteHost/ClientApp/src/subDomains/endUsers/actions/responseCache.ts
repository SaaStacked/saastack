import { CacheKeys } from '../../../framework/actions/ActionCommand.ts';


const endUserCacheKeys = {
  all: ['users'],
  memberships: {
    all: ['users', 'memberships'],
    me: ['users', 'memberships', 'me'],
    mutate: (userId: string) => [[...endUserCacheKeys.memberships.all, userId]] as CacheKeys
  },
  users: {
    all: ['users'],
    me: ['users', 'me']
  }
};

export default endUserCacheKeys;
