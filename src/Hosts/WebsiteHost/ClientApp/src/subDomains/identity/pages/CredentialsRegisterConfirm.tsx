import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';
import { AxiosError } from 'axios';
import { ExpectedErrorDetails } from '../../../framework/actions/ApiErrorState.ts';
import Alert from '../../../framework/components/alert/Alert.tsx';
import Button from '../../../framework/components/button/Button.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import Loader from '../../../framework/components/loader/Loader.tsx';
import UnhandledError from '../../../framework/components/unhandledError/UnhandledError.tsx';
import { ConfirmRegisterErrors, CredentialsRegisterConfirmAction } from '../actions/credentialsRegisterConfirm.ts';
import { CredentialsRegisterConfirmationResendAction } from '../actions/credentialsRegisterConfirmationResend.ts';


// Creates an "ephemeral" page to redirect the user to authorize against Microsoft OAuth2, and then Authenticates the user
// Receives an authCode from Microsoft OAuth2, and forwards the authCode to the API to confirm the login.
export const CredentialsRegisterConfirm: React.FC = () => {
  const { t: translate } = useTranslation();
  const [queryString] = useSearchParams();
  const token = queryString.get('token');

  const {
    execute: confirmRegistration,
    isExecuting,
    isSuccess,
    lastExpectedError,
    lastUnexpectedError
  } = CredentialsRegisterConfirmAction();

  useEffect(() => {
    if (token) {
      confirmRegistration({ token });
    }
  }, [token, confirmRegistration]);

  if (isSuccess) {
    return (
      <HandleSuccess
        translate={translate}
        lastExpectedError={lastExpectedError}
        lastUnexpectedError={lastUnexpectedError}
        isExecuting={isExecuting}
        token={token}
      />
    );
  }

  if (lastExpectedError || lastUnexpectedError) {
    return (
      <HandleErrors
        translate={translate}
        lastExpectedError={lastExpectedError}
        lastUnexpectedError={lastUnexpectedError}
        isExecuting={isExecuting}
        token={token}
      />
    );
  }

  return (
    <HandleConfirming
      translate={translate}
      lastExpectedError={lastExpectedError}
      lastUnexpectedError={lastUnexpectedError}
      isExecuting={isExecuting}
      token={token}
    />
  );
};

interface HandlerProps {
  translate: (key: string, options?: any) => string;
  lastExpectedError?: ExpectedErrorDetails<any> | undefined;
  lastUnexpectedError?: AxiosError;
  isExecuting: boolean;
  token?: string | null;
}

function HandleConfirming({ translate, isExecuting, token }: HandlerProps) {
  return (
    <FormPage title={translate('pages.identity.credentials_register_confirm.states.confirming.title')}>
      {!token ? (
        <>
          <Alert
            id="error_token_missing"
            type="error"
            message={translate('pages.identity.credentials_register_confirm.states.confirming.errors.token_missing')}
          ></Alert>
          <Link to="/" className="btn btn-secondary">
            {translate('pages.identity.credentials_register_confirm.links.home')}
          </Link>
        </>
      ) : isExecuting ? (
        <Loader
          id="confirming"
          message={translate('pages.identity.credentials_register_confirm.states.confirming.message')}
        />
      ) : null}
    </FormPage>
  );
}

function HandleSuccess({ translate }: HandlerProps) {
  return (
    <FormPage title={translate('pages.identity.credentials_register_confirm.states.success.title')}>
      <div className="text-center mb-8">
        <p className="text-lg mb-4">
          {translate('pages.identity.credentials_register_confirm.states.success.message')}
        </p>
        <Link to="/identity/credentials/login" className="btn btn-primary mr-4">
          {translate('pages.identity.credentials_register_confirm.links.login')}
        </Link>
        <Link to="/" className="btn btn-secondary">
          {translate('pages.identity.credentials_register_confirm.links.home')}
        </Link>
      </div>
    </FormPage>
  );
}

function HandleErrors({ translate, lastExpectedError, lastUnexpectedError, isExecuting, token }: HandlerProps) {
  const isTokenExpired = lastExpectedError!.code === ConfirmRegisterErrors.token_expired;
  const isTokenUsed = lastExpectedError!.code === ConfirmRegisterErrors.token_used;

  return (
    <FormPage title={translate('pages.identity.credentials_register_confirm.states.failed.title')}>
      <div className="text-center mb-8">
        {isTokenExpired && (
          <>
            <Alert id="error_token_expired" type="error">
              {translate('pages.identity.credentials_register_confirm.states.failed.errors.token_expired')}
              <HandleResend
                translate={translate}
                lastExpectedError={lastExpectedError}
                lastUnexpectedError={lastUnexpectedError}
                isExecuting={isExecuting}
                token={token}
              />{' '}
            </Alert>
          </>
        )}
        {isTokenUsed && (
          <Alert id="error_token_used" type="error">
            {translate('pages.identity.credentials_register_confirm.states.failed.errors.token_used')}
          </Alert>
        )}

        {lastUnexpectedError && <UnhandledError id="error_unexpected" error={lastUnexpectedError} />}

        <Link to="/identity/credentials/login" className="btn btn-primary mr-4">
          {translate('pages.identity.credentials_register_confirm.links.login')}
        </Link>
        <Link to="/" className="btn btn-secondary">
          {translate('pages.identity.credentials_register_confirm.links.home')}
        </Link>
      </div>
    </FormPage>
  );
}

function HandleResend({ translate, token }: HandlerProps) {
  const { execute, isExecuting, lastExpectedError, lastUnexpectedError, isSuccess } =
    CredentialsRegisterConfirmationResendAction();
  const isUnknownToken =
    lastExpectedError &&
    (lastExpectedError.code === ConfirmRegisterErrors.token_used ||
      lastExpectedError.code === ConfirmRegisterErrors.token_expired);

  return (
    <>
      <Button
        id="resend"
        variant="secondary"
        label={translate('pages.identity.credentials_register_confirm.states.resend.title')}
        busy={isExecuting}
        onClick={() => execute({ token: token ?? '' })}
      />
      {isSuccess && (
        <Alert id="resend_success" type="success">
          {translate('pages.identity.credentials_register_confirm.states.resend.success')}
        </Alert>
      )}
      {isUnknownToken && (
        <Alert id="resend_error_token_used" type="error">
          {translate('pages.identity.credentials_register_confirm.states.resend.errors.token_used')}
        </Alert>
      )}
      {lastUnexpectedError && <UnhandledError id="resend_error_unexpected" error={lastUnexpectedError} />}
    </>
  );
}
