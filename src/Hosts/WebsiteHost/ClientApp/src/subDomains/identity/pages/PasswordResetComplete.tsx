import React, { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';
import z from 'zod';
import { ErrorResponse } from '../../../framework/actions/Actions.ts';
import { ExpectedErrorDetails } from '../../../framework/actions/ApiErrorState.ts';
import { VerifyPasswordResetData } from '../../../framework/api/apiHost1';
import Alert from '../../../framework/components/alert/Alert.tsx';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormInput from '../../../framework/components/form/formInput/FormInput.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { CompletePasswordResetAction, CompletePasswordResetErrors } from '../actions/completePasswordReset.ts';
import { VerifyPasswordResetAction, VerifyPasswordResetErrors } from '../actions/verifyPasswordReset.ts';


export const PasswordResetCompletePage: React.FC = () => {
  const { t: translate } = useTranslation();
  const [queryString] = useSearchParams();
  const token = queryString.get('token');
  const verifyPasswordReset = VerifyPasswordResetAction();
  const verifyPasswordResetTrigger = useRef<PageActionRef<VerifyPasswordResetData>>(null);

  useEffect(() => {
    if (token) {
      verifyPasswordResetTrigger.current?.execute({ path: { Token: token } } as VerifyPasswordResetData);
    }
  }, [token]);

  if (!token) {
    return <HandleMissingToken translate={translate} />;
  }

  return (
    <>
      {!verifyPasswordReset.isSuccess ? (
        <FormPage title={translate('pages.identity.credentials_password_reset_complete.states.verifying.title')}>
          <PageAction
            id="password_reset_verify"
            action={verifyPasswordReset}
            ref={verifyPasswordResetTrigger}
            expectedErrorMessages={{
              [VerifyPasswordResetErrors.token_expired]: translate(
                'pages.identity.credentials_password_reset_complete.states.verifying.errors.token_expired'
              ),
              [VerifyPasswordResetErrors.token_invalid]: translate(
                'pages.identity.credentials_password_reset_complete.states.verifying.errors.token_invalid'
              )
            }}
            loadingMessage={translate('pages.identity.credentials_password_reset_complete.states.verifying.loader')}
          ></PageAction>
          <div className="text-center mt-4">
            <Link to="/identity/credentials/password-reset" className="btn btn-primary mr-4">
              {translate('pages.identity.credentials_password_reset_complete.links.request_reset')}
            </Link>
            <Link to="/" className="btn btn-secondary">
              {translate('pages.identity.credentials_password_reset_complete.links.home')}
            </Link>
          </div>
        </FormPage>
      ) : (
        <HandleVerifySuccess translate={translate} token={token} />
      )}
    </>
  );
};

interface HandlerProps {
  translate: (key: string, options?: any) => string;
  lastExpectedError?: ExpectedErrorDetails<any> | undefined;
  lastUnexpectedError?: ErrorResponse;
  token: string;
}

function HandleMissingToken({ translate }: Pick<HandlerProps, 'translate'>) {
  return (
    <FormPage title={translate('pages.identity.credentials_password_reset_complete.states.invalid.title')}>
      <Alert
        id="error_token_missing"
        type="error"
        message={translate('pages.identity.credentials_password_reset_complete.states.invalid.message')}
      />
      <div className="text-center mt-4">
        <Link to="/identity/credentials/password-reset" className="btn btn-primary mr-4">
          {translate('pages.identity.credentials_password_reset_complete.links.request_reset')}
        </Link>
        <Link to="/" className="btn btn-secondary">
          {translate('pages.identity.credentials_password_reset_complete.links.home')}
        </Link>
      </div>
    </FormPage>
  );
}

function HandleVerifySuccess({ translate, token }: HandlerProps) {
  const completePasswordReset = CompletePasswordResetAction();

  return (
    <>
      {!completePasswordReset.isSuccess ? (
        <FormPage title={translate('pages.identity.credentials_password_reset_complete.title')}>
          <p className="mb-4">{translate('pages.identity.credentials_password_reset_complete.description')}</p>
          <FormAction
            id="password_reset_complete"
            action={completePasswordReset}
            validationSchema={z
              .object({
                password: z
                  .string()
                  .min(
                    8,
                    translate('pages.identity.credentials_password_reset_complete.form.fields.password.validation')
                  )
                  .max(
                    200,
                    translate('pages.identity.credentials_password_reset_complete.form.fields.password.validation')
                  ),
                confirmPassword: z.string()
              })
              .refine((data) => data.password === data.confirmPassword, {
                message: translate(
                  'pages.identity.credentials_password_reset_complete.form.fields.confirm_password.validation'
                ),
                path: ['confirmPassword']
              })
              .transform((data) => ({ path: { Token: token }, body: { Password: data.password } }))}
            defaultValues={{
              path: {
                Token: token
              }
            }}
            expectedErrorMessages={{
              [CompletePasswordResetErrors.token_expired]: translate(
                'pages.identity.credentials_password_reset_complete.states.completing.errors.token_expired'
              ),
              [CompletePasswordResetErrors.token_invalid]: translate(
                'pages.identity.credentials_password_reset_complete.states.completing.errors.token_invalid'
              ),
              [CompletePasswordResetErrors.invalid_password]: translate(
                'pages.identity.credentials_password_reset_complete.states.completing.errors.invalid_password'
              ),
              [CompletePasswordResetErrors.duplicate_password]: translate(
                'pages.identity.credentials_password_reset_complete.states.completing.errors.duplicate_password'
              )
            }}
          >
            {/*This hidden username field to suppress the username warning in chrome since we do not have the username in this flow */}
            <input type="text" name="username" autoComplete="username" hidden />
            <FormInput
              id="password"
              name="password"
              type="password"
              label={translate('pages.identity.credentials_password_reset_complete.form.fields.password.label')}
              placeholder={translate(
                'pages.identity.credentials_password_reset_complete.form.fields.password.placeholder'
              )}
              autoComplete="new-password"
            />
            <FormInput
              id="confirmPassword"
              name="confirmPassword"
              type="password"
              label={translate('pages.identity.credentials_password_reset_complete.form.fields.confirm_password.label')}
              placeholder={translate(
                'pages.identity.credentials_password_reset_complete.form.fields.confirm_password.placeholder'
              )}
              autoComplete="new-password"
            />
            <FormSubmitButton
              label={translate('pages.identity.credentials_password_reset_complete.form.submit.label')}
            />
            <div className="text-center">
              <p>
                <Link to="/identity/credentials/login" className="btn btn-secondary">
                  {translate('pages.identity.credentials_password_reset_complete.links.login')}
                </Link>
              </p>
            </div>
          </FormAction>
        </FormPage>
      ) : (
        <HandleCompleted translate={translate} token={token} />
      )}
    </>
  );
}

function HandleCompleted({ translate }: HandlerProps) {
  return (
    <FormPage title={translate('pages.identity.credentials_password_reset_complete.states.completed.title')}>
      <div className="text-center mb-8">
        <p className="text-lg mb-4">
          {translate('pages.identity.credentials_password_reset_complete.states.completed.message')}
        </p>
        <Link to="/identity/credentials/login" className="btn btn-primary mr-4">
          {translate('pages.identity.credentials_password_reset_complete.links.login')}
        </Link>
        <Link to="/" className="btn btn-secondary">
          {translate('pages.identity.credentials_password_reset_complete.links.home')}
        </Link>
      </div>
    </FormPage>
  );
}
