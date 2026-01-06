import { useActionQuery } from '../../../framework/actions/ActionQuery';
import {
  getOAuth2ClientConsentStatusForCaller,
  GetOAuth2ClientConsentStatusForCallerResponse,
  OAuth2ClientConsentStatus,
  type GetOAuth2ClientConsentStatusForCallerData
} from '../../../framework/api/apiHost1';
import { RoutePaths } from '../../../framework/constants.ts';
import oAuth2CacheKeys from './responseCache.ts';

export const GetOAuth2ClientConsentAction = (id: string, scope: string) =>
  useActionQuery<
    GetOAuth2ClientConsentStatusForCallerData,
    GetOAuth2ClientConsentStatusForCallerResponse,
    OAuth2ClientConsentStatus
  >({
    request: (request) =>
      getOAuth2ClientConsentStatusForCaller({
        ...request,
        path: {
          Id: id
        },
        query: {
          Scope: scope
        }
      }),
    transform: (res) => res.status,
    cacheKey: oAuth2CacheKeys.client.consent.query(id),
    onSuccess: (_requestData, response, _statusCode, _headers) => {
      if (response.isConsented) {
        window.location.replace(RoutePaths.OAuth2Authorize); // no browser history
      }
    }
  });
