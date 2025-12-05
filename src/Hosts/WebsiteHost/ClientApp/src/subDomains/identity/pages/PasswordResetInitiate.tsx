import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import z from 'zod';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormInput from '../../../framework/components/form/formInput/FormInput.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import { InitiatePasswordResetAction, InitiatePasswordResetErrors } from '../actions/initiatePasswordReset.ts';

export const PasswordResetRequestPage: React.FC = () => {
  const { t: translate } = useTranslation();
  const initiateReset = InitiatePasswordResetAction();

  return (
    <FormPage title={translate('pages.identity.credentials_password_reset_initiate.title')}>
      <p className="mb-4 text-center">{translate('pages.identity.credentials_password_reset_initiate.description')}</p>
      <FormAction
        id="password_reset_request"
        action={initiateReset}
        expectedErrorMessages={{
          [InitiatePasswordResetErrors.user_not_registered]: translate(
            'pages.identity.credentials_password_reset_initiate.errors.user_not_registered'
          )
        }}
        validationSchema={z.object({
          emailAddress: z.email(
            translate('pages.identity.credentials_password_reset_initiate.form.fields.email.validation')
          )
        })}
        onSuccess={() => {
          window.location.href = '/identity/credentials/password-reset-redirect';
        }}
      >
        <FormInput
          id="emailAddress"
          name="emailAddress"
          type="email"
          label={translate('pages.identity.credentials_password_reset_initiate.form.fields.email.label')}
          placeholder={translate('pages.identity.credentials_password_reset_initiate.form.fields.email.placeholder')}
          autoComplete="username"
        />
        <FormSubmitButton label={translate('pages.identity.credentials_password_reset_initiate.form.submit.label')} />
      </FormAction>
      <div className="text-center">
        <p>
          {translate('pages.identity.credentials_password_reset_initiate.links.login.question')}{' '}
          <Link to="/identity/credentials/login">
            {translate('pages.identity.credentials_password_reset_initiate.links.login.text')}
          </Link>
        </p>
        <p>
          <Link to="/" className="btn btn-secondary">
            {translate('pages.identity.credentials_password_reset_initiate.links.home')}
          </Link>
        </p>
      </div>
    </FormPage>
  );
};
