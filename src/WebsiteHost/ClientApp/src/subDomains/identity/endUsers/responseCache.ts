const endUserCacheKeys = {
  memberships: {
    all: ['users.memberships'] as const,
    me: ['users.memberships.me'] as const
  },
  users: {
    all: ['users.all'] as const,
    me: ['users.me'] as const
  }
};

export default endUserCacheKeys;
