import React, { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useSearchParams } from 'react-router-dom';
import { AuthenticateRequest } from '../../../framework/api/websiteHost';
import Alert from '../../../framework/components/alert/Alert.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import Loader from '../../../framework/components/loader/Loader.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { recorder } from '../../../framework/recorder.ts';
import { LoginErrors } from '../actions/credentialsLogin.ts';
import { SsoLoginAction } from '../actions/ssoLogin.ts';
import {
  cleanupPKCEParameters,
  generateCodeChallenge,
  generateCodeVerifier,
  generateOAuth2State,
  storePKCEParameters,
  validatePKCEParameters
} from '../utils/OAuth2Security.ts';

interface SsoLoginPageProps {
  providerId: string;
  providerName: string;
  authorizationServerBaseUrl: string;
  pkce?: boolean;
  clientId?: string;
}

// Creates a login page for a specific SSO provider
// Redirects the browser to the SSO provider authorization page, in order to obtain an authorization code, and processes the response:
// 1. If the user denies the request, then no authCode is returned and an error from the provider is displayed.
// 2. If the user accepts the request, then an authCode is returned, which we relay to the API to authenticate the user.
// 3. If the user accepts the request, but the pkce validation fails, then an error is displayed.
export const SsoLoginPage: React.FC<SsoLoginPageProps> = ({
  providerId,
  providerName,
  authorizationServerBaseUrl,
  pkce = false,
  clientId
}: SsoLoginPageProps) => {
  const { t: translate } = useTranslation();
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const login = SsoLoginAction();
  const loginTrigger = useRef<PageActionRef<AuthenticateRequest>>(null);

  const oAuth2AuthCode = searchParams.get('code');
  const oAuth2State = searchParams.get('state');
  const oAuth2Error = searchParams.get('error');
  const oAuth2ErrorDescription = searchParams.get('error_description');
  const hasAuthenticated = useRef(false);

  useEffect(() => {
    if (oAuth2AuthCode && !hasAuthenticated.current) {
      hasAuthenticated.current = true;
      authenticateUser();
    } else if (!oAuth2Error && !hasAuthenticated.current) {
      authorizeWithProvider();
    }
  }, [oAuth2AuthCode, oAuth2Error]);

  function authenticateUser() {
    let codeVerifier = null;
    if (pkce) {
      const validation = validatePKCEParameters(oAuth2State);
      if (!validation.isValid) {
        recorder.traceInformation(
          validation.error ?? translate('pages.identity.ss_login.errors.oauth2', { error: validation.error })
        );
        return;
      }

      codeVerifier = validation.codeVerifier;
      cleanupPKCEParameters();
    }

    loginTrigger.current?.execute({
      provider: providerId,
      authCode: oAuth2AuthCode,
      codeVerifier
    } as AuthenticateRequest);
  }

  function authorizeWithProvider() {
    const state = generateOAuth2State();
    function authorizeProvider(codeChallenge: string | null) {
      const redirectUri = encodeURIComponent(`${window.location.origin}${location.pathname}`);
      const scope = encodeURIComponent('openid profile email');

      window.location.href =
        `${authorizationServerBaseUrl}?` +
        (clientId ? `client_id=${clientId}&` : '') +
        `response_type=code` +
        `&redirect_uri=${redirectUri}` +
        `&scope=${scope}` +
        `&state=${state}` +
        (codeChallenge ? `&code_challenge=${codeChallenge}&code_challenge_method=S256` : '');
    }

    if (pkce) {
      // Generate and store PKCE
      const codeVerifier = generateCodeVerifier();
      storePKCEParameters(state, codeVerifier);

      generateCodeChallenge(codeVerifier).then((codeChallenge) => authorizeProvider(codeChallenge));
    } else {
      authorizeProvider(null);
    }
  }

  if (oAuth2Error) {
    return (
      <HandleAuthorizationError
        translate={translate}
        providerName={providerName}
        oAuth2Error={oAuth2Error}
        oAuth2ErrorDescription={oAuth2ErrorDescription}
      />
    );
  }

  if (oAuth2AuthCode) {
    if (login.isExecuting) {
      return (
        <HandleAuthenticationBusy
          translate={translate}
          providerName={providerName}
          oAuth2Error={undefined}
          oAuth2ErrorDescription={null}
        />
      );
    }

    if (!login.isSuccess) {
      return (
        <FormPage title={translate('pages.identity.sso_login.title', { provider: providerName })}>
          <PageAction
            id="sso_login"
            action={login}
            expectedErrorMessages={{
              [LoginErrors.account_locked]: translate('pages.identity.sso_login.errors.account_locked'),
              [LoginErrors.account_unverified]: translate('pages.identity.sso_login.errors.account_unverified'),
              [LoginErrors.invalid_credentials]: translate('pages.identity.sso_login.errors.invalid_credentials'),
              [LoginErrors.mfa_required]: translate('pages.identity.sso_login.errors.mfa_required')
            }}
            loadingMessage={translate('pages.identity.sso_login.loaders.authenticating')}
            ref={loginTrigger}
          />
          <div className="text-center space-y-4">
            <Link to="/" className="btn btn-secondary">
              {translate('pages.identity.sso_login.links.home')}
            </Link>
          </div>
        </FormPage>
      );
    }
  }

  return (
    <HandleAuthorizationBusy
      translate={translate}
      providerName={providerName}
      oAuth2Error={undefined}
      oAuth2ErrorDescription={null}
    />
  );
};

interface HandlerProps {
  translate: (key: string, options?: any) => string;
  providerName: string;
  oAuth2Error?: string;
  oAuth2ErrorDescription: string | null;
}

function HandleAuthorizationBusy({ translate, providerName }: HandlerProps) {
  return <Loader message={translate('pages.identity.sso_login.loaders.authorizing', { provider: providerName })} />;
}

function HandleAuthorizationError({ translate, providerName, oAuth2Error, oAuth2ErrorDescription }: HandlerProps) {
  const error = oAuth2Error ?? 'unknown';
  return (
    <FormPage title={translate('pages.identity.sso_login.title', { provider: providerName })}>
      <div className="text-center space-y-4">
        <Alert
          id="oauth2_error"
          type="error"
          title={translate('pages.identity.sso_login.errors.oauth2', { provider: providerName, error })}
          message={oAuth2ErrorDescription}
        />
        <Link to="/" className="btn btn-secondary">
          {translate('pages.identity.sso_login.links.home')}
        </Link>
      </div>
    </FormPage>
  );
}

function HandleAuthenticationBusy({ translate }: HandlerProps) {
  return <Loader message={translate('pages.identity.sso_login.loaders.authenticating')} />;
}
