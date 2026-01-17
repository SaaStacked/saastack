import React, { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';
import { ErrorResponse } from '../../../framework/actions/Actions.ts';
import { ExpectedErrorDetails } from '../../../framework/actions/ApiErrorState.ts';
import { ConfirmPersonCredentialRegistrationRequest } from '../../../framework/api/apiHost1';
import Alert from '../../../framework/components/alert/Alert.tsx';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import Icon from '../../../framework/components/icon/Icon.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import UnhandledError from '../../../framework/components/unhandledError/UnhandledError.tsx';
import { RoutePaths } from '../../../framework/constants.ts';
import {
  ConfirmPersonCredentialRegistrationAction,
  ConfirmPersonCredentialRegistrationErrors
} from '../actions/confirmPersonCredentialRegistration.ts';
import { ResendPersonCredentialRegistrationConfirmationAction } from '../actions/resendPersonCredentialRegistrationConfirmation.ts';

export const CredentialsRegisterConfirm: React.FC = () => {
  const { t: translate } = useTranslation();
  const [queryString] = useSearchParams();
  const token = queryString.get('token');
  const confirmRegistration = ConfirmPersonCredentialRegistrationAction();
  const confirmRegistrationTrigger = useRef<PageActionRef<ConfirmPersonCredentialRegistrationRequest>>(null);

  useEffect(() => {
    if (token) {
      confirmRegistrationTrigger.current?.execute({ token });
    }
  }, [token]);

  if (!token) {
    return <HandleMissingToken translate={translate} />;
  }

  return (
    <>
      {!confirmRegistration.isSuccess ? (
        <FormPage>
          <PageAction
            id="credentials_register_confirm"
            action={confirmRegistration}
            ref={confirmRegistrationTrigger}
            overrideExpectedErrorMessages={true} //we need to handle the errors ourselves
            loadingMessage={translate('pages.identity.credentials_register_confirm.states.confirming.loader')}
          >
            <HandleConfirmError
              translate={translate}
              token={token}
              lastExpectedError={confirmRegistration.lastExpectedError}
              lastUnexpectedError={confirmRegistration.lastUnexpectedError}
            />
          </PageAction>
          <div className="text-center mt-4">
            <Link to={RoutePaths.Home}>{translate('pages.identity.credentials_register_confirm.links.home')}</Link>
          </div>
        </FormPage>
      ) : (
        <HandleConfirmSuccess
          translate={translate}
          lastExpectedError={confirmRegistration.lastExpectedError}
          lastUnexpectedError={confirmRegistration.lastUnexpectedError}
          token={token}
        />
      )}
    </>
  );
};

interface HandlerProps {
  translate: (key: string, options?: any) => string;
  lastExpectedError?: ExpectedErrorDetails<any> | undefined;
  lastUnexpectedError?: ErrorResponse;
  success?: boolean;
  token: string;
}

function HandleMissingToken({ translate }: Pick<HandlerProps, 'translate'>) {
  return (
    <FormPage title={translate('pages.identity.credentials_register_confirm.states.invalid.title')}>
      <Alert
        id="error_token_missing"
        type="error"
        message={translate('pages.identity.credentials_register_confirm.states.invalid.message')}
      />
      <div className="text-center mt-4">
        <Link to={RoutePaths.Home}>{translate('pages.identity.credentials_register_confirm.links.home')}</Link>
      </div>
    </FormPage>
  );
}

function HandleConfirmSuccess({ translate }: HandlerProps) {
  return (
    <FormPage title={translate('pages.identity.credentials_register_confirm.states.registered.title')}>
      <div className="text-center mb-8">
        <p className="text-lg mb-4">
          {translate('pages.identity.credentials_register_confirm.states.registered.message')}
        </p>
        <Link to={RoutePaths.CredentialsLogin} className="btn btn-primary mr-4">
          {translate('pages.identity.credentials_register_confirm.links.login')}
        </Link>
        <Link to={RoutePaths.Home}>{translate('pages.identity.credentials_register_confirm.links.home')}</Link>
      </div>
    </FormPage>
  );
}

function HandleConfirmError({ translate, lastExpectedError, lastUnexpectedError, token }: HandlerProps) {
  const isTokenExpired =
    lastExpectedError && lastExpectedError.code === ConfirmPersonCredentialRegistrationErrors.token_expired;
  const isTokenUsed =
    lastExpectedError && lastExpectedError.code === ConfirmPersonCredentialRegistrationErrors.token_used;
  const resendConfirmation = ResendPersonCredentialRegistrationConfirmationAction(token!);

  return (
    <FormPage title={translate('pages.identity.credentials_register_confirm.states.confirming.title')}>
      <div className="text-center mb-8">
        {isTokenExpired && (
          <>
            <Alert id="error_token_expired" type="error">
              {translate('pages.identity.credentials_register_confirm.states.confirming.errors.token_expired')}
              <ButtonAction
                className="p-2 rounded-full w-8 h-8"
                id="resend"
                label={translate('pages.identity.credentials_register_confirm.states.resending.title')}
                busyLabel={translate('pages.identity.credentials_register_confirm.states.resending.loader')}
                action={resendConfirmation}
                expectedErrorMessages={{
                  [ConfirmPersonCredentialRegistrationErrors.token_used]: translate(
                    'pages.identity.credentials_register_confirm.states.resending.errors.token_used'
                  ),
                  [ConfirmPersonCredentialRegistrationErrors.token_expired]: translate(
                    'pages.identity.credentials_register_confirm.states.resending.errors.token_expired'
                  )
                }}
                variant="brand-secondary"
              >
                <Icon symbol="repeat" size={16} color="white" />
                {resendConfirmation.isSuccess && (
                  <Alert id="resend_success" type="success">
                    {translate('pages.identity.credentials_register_confirm.states.resent.message')}
                  </Alert>
                )}
              </ButtonAction>
            </Alert>
          </>
        )}
        {isTokenUsed && (
          <Alert id="error_token_used" type="error">
            {translate('pages.identity.credentials_register_confirm.states.confirming.errors.token_used')}
          </Alert>
        )}

        {lastUnexpectedError && <UnhandledError id="error_unexpected" error={lastUnexpectedError} />}
      </div>
    </FormPage>
  );
}
