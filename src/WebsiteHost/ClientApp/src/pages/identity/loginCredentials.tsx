import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import z from 'zod';
import { LoginCredentialsAction, LoginErrors } from '../../actions/identity/loginCredentials.ts';
import Form from '../../components/form/Form.tsx';
import FormInput from '../../components/form/formInput/FormInput.tsx';
import FormSubmitButton from '../../components/form/formSubmitButton/FormSubmitButton.tsx';


export const LoginCredentialsPage: React.FC = () => {
  const { t: translate } = useTranslation('common');
  const login = LoginCredentialsAction();

  return (
    <div className="container min-h-screen flex items-center justify-center">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <h1 className="text-4xl font-bold text-center mb-16">{translate('pages.identity.login_credentials.title')}</h1>
        <Form
          id="login_credentials"
          action={login}
          validationSchema={z.object({
            username: z.email(translate('pages.identity.login_credentials.form.fields.username.validation')),
            password: z.string().min(8, translate('pages.identity.login_credentials.form.fields.password.validation'))
          })}
          expectedErrorMessages={{
            [LoginErrors.account_locked]: translate('pages.identity.login_credentials.errors.account_locked'),
            [LoginErrors.account_unverified]: translate('pages.identity.login_credentials.errors.account_unverified'),
            [LoginErrors.invalid_credentials]: translate('pages.identity.login_credentials.errors.invalid_credentials')
          }}
        >
          <FormInput
            id="username"
            name="username"
            label={translate('pages.identity.login_credentials.form.fields.username.label')}
            placeholder={translate('pages.identity.login_credentials.form.fields.username.placeholder')}
            autoComplete="username"
          />
          <FormInput
            id="password"
            name="password"
            type="password"
            label={translate('pages.identity.login_credentials.form.fields.password.label')}
            placeholder={translate('pages.identity.login_credentials.form.fields.password.placeholder')}
            autoComplete="current-password"
          />
          <FormSubmitButton label={translate('pages.identity.login_credentials.form.submit.label')} />
        </Form>
        <div className="text-center">
          <p>
            <Link to="/" className="btn btn-secondary">
              {translate('pages.identity.login_credentials.links.home')}
            </Link>
          </p>
          <p>
            {translate('pages.identity.login_credentials.links.register.question')}{' '}
            <Link to="/identity/register-credentials">
              {translate('pages.identity.login_credentials.links.register.text')}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};
