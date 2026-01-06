import { useActionCommand } from '../../../framework/actions/ActionCommand';
import {
  consentOAuth2Client,
  ConsentOAuth2ClientRequest,
  ConsentOAuth2ClientResponse
} from '../../../framework/api/websiteHost';
import { RoutePaths } from '../../../framework/constants.ts';
import oAuth2CacheKeys from './responseCache.ts';

export const ConsentOAuth2ClientAction = (
  clientId: string,
  redirectUri: string,
  scope: string,
  state: string | null,
  consent: boolean
) =>
  useActionCommand<ConsentOAuth2ClientRequest, ConsentOAuth2ClientResponse>({
    request: (request) =>
      consentOAuth2Client({
        body: {
          ...request,
          consented: consent,
          redirectUri,
          scope,
          state
        },
        path: {
          Id: clientId
        }
      }),
    onSuccess: (_requestData, response) => {
      if (response.redirect.isConsented) {
        window.location.href = RoutePaths.OAuth2Authorize; // no browser history
      } else {
        window.location.replace(response.redirect.redirectUri); // no browser history
      }
    },
    invalidateCacheKeys: oAuth2CacheKeys.client.consent.mutate(clientId)
  });
