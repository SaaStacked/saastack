import { TenantedCacheKey } from '../../../framework/constants.ts';

const subscriptionCacheKeys = {
  all: ['subscriptions'],
  plans: {
    query: ['subscriptions', 'pricing']
  },
  subscription: {
    all: [TenantedCacheKey, 'subscriptions'],
    query: (organizationId: string) => [...subscriptionCacheKeys.subscription.all, organizationId],
    mutate: (organizationId: string) => [subscriptionCacheKeys.subscription.query(organizationId)]
  }
};

export default subscriptionCacheKeys;
