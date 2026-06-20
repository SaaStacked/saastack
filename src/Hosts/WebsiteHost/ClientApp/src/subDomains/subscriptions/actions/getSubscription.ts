import type { GetSubscriptionData, GetSubscriptionResponse, SubscriptionWithPlan } from '@/framework/api/apiHost1';
import { getSubscription } from '@/framework/api/apiHost1';
import { useActionQuery } from '../../../framework/actions/ActionQuery';
import subscriptionCacheKeys from './responseCache';

export enum SubscriptionErrorCodes {
  not_billingadmin = 'not_billingadmin'
}

export function GetSubscriptionAction() {
  return useActionQuery<GetSubscriptionData, GetSubscriptionResponse, SubscriptionWithPlan, SubscriptionErrorCodes>({
    request: request => getSubscription(request),
    transform: res => res.subscription,
    passThroughErrors: {
      403: SubscriptionErrorCodes.not_billingadmin
    },
    cacheKey: request => subscriptionCacheKeys.subscription.query(request.path.Id)
  });
}
