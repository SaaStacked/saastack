import type { ListPricingPlansResponse, PricingPlans } from '@/framework/api/apiHost1';
import { listPricingPlans } from '@/framework/api/apiHost1';
import type { EmptyRequest } from '@/framework/api/EmptyRequest.ts';

import { useActionQuery } from '../../../framework/actions/ActionQuery';
import subscriptionCacheKeys from './responseCache';

export function ListPricingPlansAction() {
  return useActionQuery<EmptyRequest, ListPricingPlansResponse, PricingPlans>({
    request: request => listPricingPlans(request),
    transform: res => res.plans,
    cacheKey: subscriptionCacheKeys.plans.query,
    cachePeriodMs: 3_600_000 // 1 hour
  });
}
