import { TenantedCacheKey } from '../../../framework/constants.ts';
import endUserCacheKeys from '../../endUsers/actions/responseCache.ts';
import organizationCacheKeys from '../../organizations/actions/responseCache.ts';
import userProfileCacheKeys from '../../userProfiles/actions/responseCache.ts';

const identityCacheKeys = {
  all: [],
  clearSession: () => [
    [TenantedCacheKey], // clears all tenanted resources
    userProfileCacheKeys.all,
    endUserCacheKeys.all,
    organizationCacheKeys.all
  ],
  oauth: {
    all: ['oauth2'],
    client: {
      all: ['oauth2', 'clients'],
      query: (clientId: string) => [...identityCacheKeys.oauth.client.all, clientId],
      mutate: (clientId: string) => [identityCacheKeys.oauth.client.query(clientId)],
      consent: {
        all: ['oauth2', 'clients', 'consents'],
        query: (clientId: string) => [...identityCacheKeys.oauth.client.consent.all, clientId],
        mutate: (clientId: string) => [identityCacheKeys.oauth.client.consent.query(clientId)]
      }
    }
  }
};

export default identityCacheKeys;
