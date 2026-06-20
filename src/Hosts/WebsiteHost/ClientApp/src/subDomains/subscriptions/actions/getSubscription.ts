import type { GetSubscriptionData, GetSubscriptionResponse, SubscriptionWithPlan } from '@/framework/api/apiHost1';
import { getSubscription } from '@/framework/api/apiHost1';
import { useActionQuery } from '../../../framework/actions/ActionQuery';
import subscriptionCacheKeys from './responseCache';

export enum SubscriptionErrorCodes {
  forbidden = 'forbidden'
}

export function GetSubscriptionAction() {
  return useActionQuery<GetSubscriptionData, GetSubscriptionResponse, SubscriptionWithPlan, SubscriptionErrorCodes>({
    request: request => getSubscription(request),
    transform: res => res.subscription,
    passThroughErrors: {
      403: SubscriptionErrorCodes.forbidden
    },
    cacheKey: request => subscriptionCacheKeys.subscription.query(request.path.Id)
  });
}
