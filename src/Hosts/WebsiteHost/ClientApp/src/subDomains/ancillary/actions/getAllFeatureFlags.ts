import { useActionQuery } from '../../../framework/actions/ActionQuery';
import { FeatureFlag } from '../../../framework/api/apiHost1';
import { getAllFeatureFlags, GetAllFeatureFlagsResponse } from '../../../framework/api/websiteHost';
import { EmptyRequest } from '../../../framework/api/websiteHost/emptyRequest.ts';
import ancillaryCacheKeys from './responseCache.ts';

export const GetAllFeatureFlagsAction = () =>
  useActionQuery<EmptyRequest, GetAllFeatureFlagsResponse, FeatureFlag[]>({
    request: (request) => getAllFeatureFlags(request),
    transform: (res) => res.flags,
    cacheKey: ancillaryCacheKeys.features.all
  });
