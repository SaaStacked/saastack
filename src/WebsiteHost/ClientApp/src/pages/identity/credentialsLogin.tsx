import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import z from 'zod';
import { CredentialsLoginAction, LoginErrors } from '../../actions/identity/credentialsLogin.ts';
import Form from '../../components/form/Form.tsx';
import FormInput from '../../components/form/formInput/FormInput.tsx';
import FormSubmitButton from '../../components/form/formSubmitButton/FormSubmitButton.tsx';


export const CredentialsLoginPage: React.FC = () => {
  const { t: translate } = useTranslation('common');
  const login = CredentialsLoginAction();

  return (
    <div className="container min-h-screen flex items-center justify-center">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <h1 className="text-4xl font-bold text-center mb-16">{translate('pages.identity.credentials_login.title')}</h1>
        <Form
          id="credentials_login"
          action={login}
          validationSchema={z.object({
            username: z.email(translate('pages.identity.credentials_login.form.fields.username.validation')),
            password: z.string().min(8, translate('pages.identity.credentials_login.form.fields.password.validation'))
          })}
          expectedErrorMessages={{
            [LoginErrors.account_locked]: translate('pages.identity.credentials_login.errors.account_locked'),
            [LoginErrors.account_unverified]: translate('pages.identity.credentials_login.errors.account_unverified'),
            [LoginErrors.invalid_credentials]: translate('pages.identity.credentials_login.errors.invalid_credentials')
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
        </Form>
        <div className="text-center">
          <p>
            <Link to="/" className="btn btn-secondary">
              {translate('pages.identity.credentials_login.links.home')}
            </Link>
          </p>
          <p>
            {translate('pages.identity.credentials_login.links.register.question')}{' '}
            <Link to="/identity/credentials/register">
              {translate('pages.identity.credentials_login.links.register.text')}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};
