import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import z from 'zod';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormInput from '../../../framework/components/form/formInput/FormInput.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import { CredentialsLoginAction, LoginErrors } from '../actions/credentialsLogin.ts';


export const CredentialsLoginPage: React.FC = () => {
  const { t: translate } = useTranslation();
  const login = CredentialsLoginAction();

  return (
    <FormPage title={translate('pages.identity.credentials_login.title')}>
      <FormAction
        id="credentials_login"
        action={login}
        validationSchema={z.object({
          username: z.email(translate('pages.identity.credentials_login.form.fields.username.validation')),
          password: z.string().min(8, translate('pages.identity.credentials_login.form.fields.password.validation'))
        })}
        expectedErrorMessages={{
          [LoginErrors.account_locked]: translate('pages.identity.credentials_login.errors.account_locked'),
          [LoginErrors.account_unverified]: translate('pages.identity.credentials_login.errors.account_unverified'),
          [LoginErrors.invalid_credentials]: translate('pages.identity.credentials_login.errors.invalid_credentials'),
          [LoginErrors.mfa_required]: translate('pages.identity.credentials_login.errors.mfa_required')
        }}
      >
        <FormInput
          id="username"
          name="username"
          label={translate('pages.identity.credentials_login.form.fields.username.label')}
          placeholder={translate('pages.identity.credentials_login.form.fields.username.placeholder')}
          autoComplete="username"
        />
        <FormInput
          id="password"
          name="password"
          type="password"
          label={translate('pages.identity.credentials_login.form.fields.password.label')}
          placeholder={translate('pages.identity.credentials_login.form.fields.password.placeholder')}
          autoComplete="current-password"
        />
        <FormSubmitButton label={translate('pages.identity.credentials_login.form.submit.label')} />
      </FormAction>
      <div className="text-center">
        <p>
          {translate('pages.identity.credentials_login.links.register.question')}{' '}
          <Link to="/identity/credentials/register">
            {translate('pages.identity.credentials_login.links.register.text')}
          </Link>
        </p>
        <p>
          <Link to="/" className="btn btn-secondary">
            {translate('pages.identity.credentials_login.links.home')}
          </Link>
        </p>
      </div>
    </FormPage>
  );
};
