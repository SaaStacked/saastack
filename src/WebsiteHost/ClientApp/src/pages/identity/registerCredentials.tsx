import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import z from 'zod';
import { RegisterCredentialsAction } from '../../actions/identity/registerCredentials.ts';
import Form from '../../components/form/Form.tsx';
import FormCheckbox from '../../components/form/formCheckbox/FormCheckbox.tsx';
import FormInput from '../../components/form/formInput/FormInput.tsx';
import FormSubmitButton from '../../components/form/formSubmitButton/FormSubmitButton.tsx';
import { getBrowserCountry, getBrowserLocale, getBrowserTimezone } from '../../utils/browser.ts';


export const RegisterCredentialsPage: React.FC = () => {
  const { t: translate } = useTranslation('common');
  const register = RegisterCredentialsAction();

  return (
    <div className="container min-h-screen flex items-center justify-center">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <h1 className="text-4xl font-bold text-center mb-16">
          {translate('pages.identity.register_credentials.title')}
        </h1>
        <Form
          id="register_credentials"
          action={register}
          validationSchema={z
            .object({
              firstName: z
                .string()
                .min(2, translate('pages.identity.register_credentials.form.fields.first_name.validation')),
              lastName: z
                .string()
                .min(2, translate('pages.identity.register_credentials.form.fields.last_name.validation')),
              emailAddress: z.email(
                translate('pages.identity.register_credentials.form.fields.email_address.validation')
              ),
              password: z
                .string()
                .min(8, translate('pages.identity.register_credentials.form.fields.password.validation')),
              confirmPassword: z.string(),
              termsAndConditionsAccepted: z.literal(
                true,
                translate('pages.identity.register_credentials.form.fields.terms.validation')
              ),
              locale: z.string().optional(),
              timezone: z.string().optional(),
              countryCode: z.string().optional()
            })
            .refine((data) => data.password === data.confirmPassword, {
              message: translate('pages.identity.register_credentials.form.fields.confirm_password.validation'),
              path: ['confirmPassword']
            })}
          defaultValues={{
            timezone: getBrowserTimezone(),
            locale: getBrowserLocale(),
            countryCode: getBrowserCountry()
          }}
          onSuccess={() => {
            window.location.href = '/identity/after-register-credentials';
          }}
        >
          <FormInput
            id="firstName"
            name="firstName"
            label={translate('pages.identity.register_credentials.form.fields.first_name.label')}
            placeholder={translate('pages.identity.register_credentials.form.fields.first_name.placeholder')}
          />
          <FormInput
            id="lastName"
            name="lastName"
            label={translate('pages.identity.register_credentials.form.fields.last_name.label')}
            placeholder={translate('pages.identity.register_credentials.form.fields.last_name.placeholder')}
          />
          <FormInput
            id="emailAddress"
            name="emailAddress"
            type="email"
            label={translate('pages.identity.register_credentials.form.fields.email_address.label')}
            placeholder={translate('pages.identity.register_credentials.form.fields.email_address.placeholder')}
          />
          <FormInput
            id="password"
            name="password"
            type="password"
            label={translate('pages.identity.register_credentials.form.fields.password.label')}
            placeholder={translate('pages.identity.register_credentials.form.fields.password.placeholder')}
            autoComplete="current-password"
          />
          <FormInput
            id="confirmPassword"
            name="confirmPassword"
            type="password"
            label={translate('pages.identity.register_credentials.form.fields.confirm_password.label')}
            placeholder={translate('pages.identity.register_credentials.form.fields.confirm_password.placeholder')}
            autoComplete="current-password"
          />
          <FormCheckbox
            id="termsAndConditionsAccepted"
            name="termsAndConditionsAccepted"
            label={translate('pages.identity.register_credentials.form.fields.terms.label')}
          />
          <p className="text-sm text-gray-500">
            Read our{' '}
            <a href="/terms" target="_blank">
              terms
            </a>{' '}
            and{' '}
            <a href="/privacy" target="_blank">
              privacy
            </a>{' '}
            for using SaaStack
          </p>
          <FormSubmitButton label={translate('pages.identity.register_credentials.form.submit.label')} />
        </Form>
        <div className="text-center">
          <p>
            <Link to="/" className="btn btn-secondary">
              {translate('pages.identity.register_credentials.links.home')}
            </Link>
          </p>
          <p>
            {translate('pages.identity.register_credentials.links.login.question')}{' '}
            <Link to="/identity/login-credentials">
              {translate('pages.identity.register_credentials.links.login.text')}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};
