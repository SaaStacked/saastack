import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';
import { AxiosError } from 'axios';
import { ExpectedErrorDetails } from '../../actions/ApiErrorState.ts';
import {
  ConfirmRegisterErrors,
  CredentialsRegisterConfirmAction
} from '../../actions/identity/credentialsRegisterConfirm.ts';
import { CredentialsRegisterConfirmationResendAction } from '../../actions/identity/credentialsRegisterConfirmationResend.ts';
import Alert from '../../components/alert/Alert.tsx';
import Button from '../../components/button/Button.tsx';
import Loader from '../../components/loader/Loader.tsx';
import UnhandledError from '../../components/unhandledError/UnhandledError.tsx';


// Creates a confirmation page for the user to confirm their credentials registration
// Accepts a "token" in the query string, from a user clicking on a link in an email.
// Sends the token to the API to confirm the registration, and processes the response:
// 1. If the token is valid, and the user has not yet registered, then the user is asked to sign in.
// 2. If the token is valid, but the user has already registered, then the user is shown an error message.
// 3. The token is invalid (perhaps expired, or unknown) then the user is shown an error message, and a link to resend the confirmation email.
export const CredentialsRegisterConfirm: React.FC = () => {
  const { t: translate } = useTranslation('common');
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
  translate: (key: string) => string;
  lastExpectedError?: ExpectedErrorDetails<any> | undefined;
  lastUnexpectedError?: AxiosError;
  isExecuting: boolean;
  token?: string | null;
}

function HandleConfirming({ translate, isExecuting, token }: HandlerProps) {
  return (
    <div className="container min-h-screen flex items-center justify-center">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <h1 className="text-4xl font-bold text-center mb-16">
          {translate('pages.identity.credentials_register_confirm.states.confirming.title')}
        </h1>

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
      </div>
    </div>
  );
}

function HandleSuccess({ translate }: HandlerProps) {
  return (
    <div className="container min-h-screen flex items-center justify-center">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <h1 className="text-4xl font-bold text-center mb-16 text-green-600">
          {translate('pages.identity.credentials_register_confirm.states.success.title')}
        </h1>
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
      </div>
    </div>
  );
}

function HandleErrors({ translate, lastExpectedError, lastUnexpectedError, isExecuting, token }: HandlerProps) {
  const isTokenExpired = lastExpectedError!.code === ConfirmRegisterErrors.token_expired;
  const isTokenUsed = lastExpectedError!.code === ConfirmRegisterErrors.token_used;

  return (
    <div className="container min-h-screen flex items-center justify-center">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <h1 className="text-4xl font-bold text-center mb-16 text-red-600">
          {translate('pages.identity.credentials_register_confirm.states.failed.title')}
        </h1>
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
      </div>
    </div>
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
