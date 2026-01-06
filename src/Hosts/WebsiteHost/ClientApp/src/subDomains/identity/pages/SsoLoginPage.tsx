import React, { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useSearchParams } from 'react-router-dom';
import { ActionResult } from '../../../framework/actions/Actions.ts';
import { AuthenticateRequest, AuthenticateResponse } from '../../../framework/api/websiteHost';
import Alert from '../../../framework/components/alert/Alert.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import Loader from '../../../framework/components/loader/Loader.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { RoutePaths } from '../../../framework/constants.ts';
import { recorder } from '../../../framework/recorder.ts';
import { LoginSsoAction, LoginSsoErrors } from '../actions/loginSso.ts';
import { generateCodeChallenge, generateCodeVerifier, generateOAuth2State, storePKCEParameters, validatePKCEParametersFromStorage } from '../utils/OAuth2Security.ts';


interface SsoLoginPageProps {
  providerId: string;
  providerName: string;
  authorizationServerBaseUrl: string;
  pkce?: boolean;
  clientId?: string;
}

enum Stage {
  Authorizing,
  Authenticating
}

// Creates a sign-in process for a specific SSO provider
// Two discrete stages:
// 1. Authorizing: User-invoked, automatic-redirect to the SSO provider's authorization page:
// 1a. This page displays progress, while calculating the PKCE code challenge (if needed), before the redirect.
// 1b. Redirects, and provider authorizes the user (on their site), ultimately obtaining an authorization code.
// 1b. Provider redirects the user back to this page, with the authorization code or error.
// 2. Authenticating: Browser-invoked redirect to this page:
// 2a. Receive the authorization code, or authorization error.
// 2b. Displays the error, if any.
// 2c. Automatically, authenticates the user with the backend API, and displays progress, and handles the response.
// 2d. If the response is an error, display the error
// 2e. The response is good, then MUST reload the home page (to obtain new cookies, and CSRF tokens).
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
  const login = LoginSsoAction();

  const oAuth2AuthCode = searchParams.get('code');
  const oAuth2State = searchParams.get('state');
  const oAuth2Error = searchParams.get('error');
  const oAuth2ErrorDescription = searchParams.get('error_description');
  const hasAuthenticated = useRef(false);
  const stage = oAuth2AuthCode || oAuth2Error ? Stage.Authenticating : Stage.Authorizing;
  const authenticateTrigger = useRef<PageActionRef<AuthenticateRequest>>(null);

  useEffect(() => {
    if (stage == Stage.Authorizing && !hasAuthenticated.current) {
      authorizeWithProvider();
    }
  }, [oAuth2AuthCode, oAuth2Error]);

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
        (codeChallenge ? `&code_challenge=${codeChallenge}&code_challenge_method=S256` : ''); // no browser history
    }

    if (pkce) {
      const codeVerifier = generateCodeVerifier();
      storePKCEParameters(state, codeVerifier);

      generateCodeChallenge(codeVerifier).then((codeChallenge) => authorizeProvider(codeChallenge));
    } else {
      authorizeProvider(null);
    }
  }

  if (stage == Stage.Authenticating) {
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
      return (
        <HandleAuthentication
          translate={translate}
          providerId={providerId}
          providerName={providerName}
          oAuth2AuthCode={oAuth2AuthCode}
          oAuth2State={oAuth2State}
          pkce={pkce}
          loginAction={login}
          authenticateTrigger={authenticateTrigger}
        />
      );
    }
  }

  return (
    <HandleProviderAuthorizationBusy
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

interface AuthenticateHandlerProps {
  translate: (key: string, options?: any) => string;
  providerId: string;
  providerName: string;
  oAuth2AuthCode: string;
  oAuth2State: string | null;
  pkce: boolean;
  loginAction: ActionResult<AuthenticateRequest, LoginSsoErrors, AuthenticateResponse>;
  authenticateTrigger: React.RefObject<PageActionRef<AuthenticateRequest> | null>;
}

function HandleProviderAuthorizationBusy({ translate, providerName }: HandlerProps) {
  return (
    <Loader
      type="page"
      message={translate('pages.identity.sso_login.loaders.authorizing', { provider: providerName })}
    />
  );
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
        <Link to={RoutePaths.Home} className="btn btn-secondary">
          {translate('pages.identity.sso_login.links.home')}
        </Link>
      </div>
    </FormPage>
  );
}

function HandleAuthentication({
  translate,
  providerId,
  providerName,
  oAuth2AuthCode,
  oAuth2State,
  pkce,
  loginAction,
  authenticateTrigger
}: AuthenticateHandlerProps) {
  const hasExecuted = useRef(false);

  let codeVerifier = null;
  if (pkce) {
    const validation = validatePKCEParametersFromStorage(oAuth2State);
    if (!validation.isValid) {
      const errorMessage =
        validation.error ?? translate('pages.identity.sso_login.errors.oauth2', { error: validation.error });
      recorder.traceInformation(errorMessage);

      return (
        <FormPage title={translate('pages.identity.sso_login.title', { provider: providerName })}>
          <div className="text-center space-y-4">
            <Alert
              id="pkce_validation_error"
              type="error"
              title={translate('pages.identity.sso_login.errors.oauth2', {
                provider: providerName,
                error: 'PKCE validation failed'
              })}
              message={errorMessage}
            />
            <Link to={RoutePaths.Home} className="btn btn-secondary">
              {translate('pages.identity.sso_login.links.home')}
            </Link>
          </div>
        </FormPage>
      );
    }

    codeVerifier = validation.codeVerifier;
  }

  const requestData = {
    provider: providerId,
    authCode: oAuth2AuthCode,
    codeVerifier
  } as AuthenticateRequest;

  useEffect(() => {
    if (!hasExecuted.current) {
      hasExecuted.current = true;
      authenticateTrigger.current?.execute(requestData);
    }
  }, [providerId, oAuth2AuthCode, codeVerifier]);

  return (
    <FormPage title={translate('pages.identity.sso_login.title', { provider: providerName })}>
      <PageAction
        id="sso_login"
        action={loginAction}
        expectedErrorMessages={{
          [LoginSsoErrors.unauthorized]: translate('pages.identity.sso_login.errors.unauthorized', {
            provider: providerName
          })
        }}
        loadingMessage={translate('pages.identity.sso_login.loaders.authenticating')}
        ref={authenticateTrigger}
      />
      <div className="text-center space-y-4">
        <Link to={RoutePaths.Home} className="btn btn-secondary">
          {translate('pages.identity.sso_login.links.home')}
        </Link>
      </div>
    </FormPage>
  );
}
