import { CacheKeys } from '../../../framework/actions/ActionCommand.ts';

const oAuth2CacheKeys = {
  all: ['oauth2'],
  client: {
    all: ['oauth2', 'clients'],
    query: (clientId: string) => [...oAuth2CacheKeys.client.all, clientId],
    mutate: (clientId: string) => [oAuth2CacheKeys.client.query(clientId)] as CacheKeys,
    consent: {
      all: ['oauth2', 'clients', 'consents'],
      query: (clientId: string) => [...oAuth2CacheKeys.client.consent.all, clientId],
      mutate: (clientId: string) => [oAuth2CacheKeys.client.consent.query(clientId)] as CacheKeys
    }
  }
};

export default oAuth2CacheKeys;
