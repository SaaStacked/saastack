import type { CacheKeys } from '../../../framework/actions/ActionCommand.ts';

const subscriptionCacheKeys = {
  all: ['subscriptions'],
  plans: {
    query: ['subscriptions', 'pricing']
  },
  subscription: {
    query: (organizationId: string) => [...subscriptionCacheKeys.all, organizationId],
    mutate: (organizationId: string) => [subscriptionCacheKeys.subscription.query(organizationId)] as CacheKeys
  }
};

export default subscriptionCacheKeys;
