import useActionQuery from '../../../framework/actions/ActionQuery';
import { getAllFeatureFlags, GetAllFeatureFlagsResponse, getFeatureFlagForCaller, GetFeatureFlagForCallerData, GetFeatureFlagForCallerResponse } from '../../../framework/api/websiteHost';
import { EmptyRequest } from '../../../framework/api/websiteHost/emptyRequest.ts';
import featureFlagCacheKeys from './responseCache.ts';


export const FeatureFlagAction = (name: string) =>
  useActionQuery<GetFeatureFlagForCallerData, GetFeatureFlagForCallerResponse>({
    request: (request) =>
      getFeatureFlagForCaller({
        ...request,
        path: {
          ...request.path,
          Name: name
        }
      }),
    transform: (res) => res.flag,
    cacheKey: featureFlagCacheKeys.flag(name)
  });

export const FeatureFlagsAction = () =>
  useActionQuery<EmptyRequest, GetAllFeatureFlagsResponse>({
    request: (request) => getAllFeatureFlags(request),
    transform: (res) => res.flags,
    cacheKey: featureFlagCacheKeys.all
  });
