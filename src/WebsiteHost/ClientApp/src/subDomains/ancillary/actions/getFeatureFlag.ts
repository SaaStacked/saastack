import { useActionQuery } from '../../../framework/actions/ActionQuery';
import { FeatureFlag } from '../../../framework/api/apiHost1';
import {
  getFeatureFlagForCaller,
  GetFeatureFlagForCallerData,
  GetFeatureFlagForCallerResponse
} from '../../../framework/api/websiteHost';
import ancillaryCacheKeys from './responseCache.ts';

export const GetFeatureFlagAction = (name: string) =>
  useActionQuery<GetFeatureFlagForCallerData, GetFeatureFlagForCallerResponse, FeatureFlag>({
    request: (request) =>
      getFeatureFlagForCaller({
        ...request,
        path: {
          ...request.path,
          Name: name
        }
      }),
    transform: (res) => res.flag,
    cacheKey: ancillaryCacheKeys.features.query(name)
  });
