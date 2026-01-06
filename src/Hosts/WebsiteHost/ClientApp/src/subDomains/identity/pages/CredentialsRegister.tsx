import React from 'react';
import { Trans, useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import z from 'zod';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormCheckbox from '../../../framework/components/form/formCheckbox/FormCheckbox.tsx';
import FormInput from '../../../framework/components/form/formInput/FormInput.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import { RoutePaths } from '../../../framework/constants.ts';
import { getBrowserCountry, getBrowserLocale, getBrowserTimezone } from '../../../framework/utils/browser.ts';
import { RegisterCredentialsAction } from '../actions/registerCredentials.ts';


export const CredentialsRegisterPage: React.FC = () => {
  const { t: translate } = useTranslation();
  const register = RegisterCredentialsAction();
  return (
    <FormPage title={translate('pages.identity.credentials_register.title')}>
      <FormAction
        id="credentials_register"
        action={register}
        validationSchema={z
          .object({
            firstName: z
              .string()
              .min(1, translate('pages.identity.credentials_register.form.fields.first_name.validation'))
              .max(100, translate('pages.identity.credentials_register.form.fields.first_name.validation')),
            lastName: z
              .string()
              .min(1, translate('pages.identity.credentials_register.form.fields.last_name.validation'))
              .max(100, translate('pages.identity.credentials_register.form.fields.last_name.validation')),
            emailAddress: z.email(
              translate('pages.identity.credentials_register.form.fields.email_address.validation')
            ),
            password: z
              .string()
              .min(8, translate('pages.identity.credentials_register.form.fields.password.validation'))
              .max(200, translate('pages.identity.credentials_register.form.fields.password.validation')),
            confirmPassword: z.string(),
            termsAndConditionsAccepted: z.literal(
              true,
              translate('pages.identity.credentials_register.form.fields.terms.validation')
            ),
            locale: z.string().optional(),
            timezone: z.string().optional(),
            countryCode: z.string().optional()
          })
          .refine((data) => data.password === data.confirmPassword, {
            message: translate('pages.identity.credentials_register.form.fields.confirm_password.validation'),
            path: ['confirmPassword']
          })}
        defaultValues={{
          timezone: getBrowserTimezone(),
          locale: getBrowserLocale(),
          countryCode: getBrowserCountry()
        }}
        onSuccess={() => {
          window.location.replace(RoutePaths.RegisterRedirect); // no browser history
        }}
      >
        <FormInput
          id="firstName"
          name="firstName"
          label={translate('pages.identity.credentials_register.form.fields.first_name.label')}
          placeholder={translate('pages.identity.credentials_register.form.fields.first_name.placeholder')}
        />
        <FormInput
          id="lastName"
          name="lastName"
          label={translate('pages.identity.credentials_register.form.fields.last_name.label')}
          placeholder={translate('pages.identity.credentials_register.form.fields.last_name.placeholder')}
        />
        <FormInput
          id="emailAddress"
          name="emailAddress"
          type="email"
          label={translate('pages.identity.credentials_register.form.fields.email_address.label')}
          placeholder={translate('pages.identity.credentials_register.form.fields.email_address.placeholder')}
          autoComplete="username"
        />
        <FormInput
          id="password"
          name="password"
          type="password"
          label={translate('pages.identity.credentials_register.form.fields.password.label')}
          placeholder={translate('pages.identity.credentials_register.form.fields.password.placeholder')}
          autoComplete="current-password"
        />
        <FormInput
          id="confirmPassword"
          name="confirmPassword"
          type="password"
          label={translate('pages.identity.credentials_register.form.fields.confirm_password.label')}
          placeholder={translate('pages.identity.credentials_register.form.fields.confirm_password.placeholder')}
          autoComplete="current-password"
        />
        <FormCheckbox id="termsAndConditionsAccepted" name="termsAndConditionsAccepted">
          <Trans
            i18nKey="pages.identity.credentials_register.form.fields.terms.label"
            values={{
              terms: translate('pages.identity.credentials_register.links.terms'),
              privacy: translate('pages.identity.credentials_register.links.privacy')
            }}
            components={{
              1: <a href={RoutePaths.Terms} target="_blank"></a>,
              2: <a href={RoutePaths.Privacy} target="_blank"></a>
            }}
          />
        </FormCheckbox>
        <FormSubmitButton label={translate('pages.identity.credentials_register.form.submit.label')} />
      </FormAction>
      <div className="text-center">
        <p>
          {translate('pages.identity.credentials_register.links.login.question')}{' '}
          <Link to={RoutePaths.CredentialsLogin}>
            {translate('pages.identity.credentials_register.links.login.text')}
          </Link>
        </p>
        <p>
          <Link to={RoutePaths.Home} className="btn btn-secondary">
            {translate('pages.identity.credentials_register.links.home')}
          </Link>
        </p>
      </div>
    </FormPage>
  );
};
