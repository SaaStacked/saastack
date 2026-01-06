import React, { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { AuthorizeOAuth2Request, OAuth2ResponseType, OpenIdConnectCodeChallengeMethod } from '../../../framework/api/websiteHost';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import Loader from '../../../framework/components/loader/Loader.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { AuthorizeOAuth2Action, AuthorizeOAuth2Errors } from '../actions/authorizeOAuth2.ts';


export const OAuth2AuthorizePage: React.FC = () => {
  const { t: translate } = useTranslation();
  const [searchParams] = useSearchParams();
  const clientId = searchParams.get('client_id') ?? '';
  const redirectUri = searchParams.get('redirect_uri') ?? '';
  const responseType = searchParams.get('response_type') ?? OAuth2ResponseType.CODE;
  const scope = searchParams.get('scope') ?? '';
  const state = searchParams.get('state');
  const nonce = searchParams.get('nonce');
  const codeChallenge = searchParams.get('code_challenge');
  const codeChallengeMethod = searchParams.get('code_challenge_method');
  const authorize = AuthorizeOAuth2Action();
  const authorizeTrigger = useRef<PageActionRef<AuthorizeOAuth2Request>>(null);

  useEffect(
    () =>
      authorizeTrigger.current?.execute({
        clientId,
        redirectUri,
        responseType: responseType as OAuth2ResponseType,
        scope,
        state,
        nonce,
        codeChallenge,
        codeChallengeMethod: codeChallengeMethod as OpenIdConnectCodeChallengeMethod
      }),
    [clientId, redirectUri, responseType, scope, state, nonce, codeChallenge, codeChallengeMethod]
  );

  return (
    <FormPage title={translate('pages.identity.oauth2_authorize.title')}>
      <PageAction
        id="oauth2_authorize"
        action={authorize}
        ref={authorizeTrigger}
        expectedErrorMessages={{
          [AuthorizeOAuth2Errors.BadRequest]: translate('pages.identity.oauth2_authorize.errors.bad_request', {
            error: authorize.lastExpectedError?.response?.detail
          })
        }}
        loadingMessage={translate('pages.identity.oauth2_authorize.loader')}
      >
        <Loader
          id="oauth2_authorize_loader"
          message={translate('pages.identity.oauth2_authorize.states.redirected.title')}
          type="inline"
        />
      </PageAction>
    </FormPage>
  );
};
