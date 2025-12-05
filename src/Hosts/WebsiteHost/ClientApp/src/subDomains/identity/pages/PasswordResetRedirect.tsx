import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import Icon from '../../../framework/components/icon/Icon.tsx';

export const PasswordResetRedirectPage: React.FC = () => {
  const { t: translate } = useTranslation();

  return (
    <FormPage title={translate('pages.identity.credentials_password_reset_redirect.title')}>
      <h2 className="text-2xl font-bold text-center mb-8">
        {translate('pages.identity.credentials_password_reset_redirect.confirmation_message.title')}
      </h2>
      <p>{translate('pages.identity.credentials_password_reset_redirect.confirmation_message.message')}</p>
      <div className="flex justify-center">
        <Icon symbol="email" size={96} color="accent" />
      </div>
      <h3 className="text-2xl font-bold text-center mt-4">
        {translate('pages.identity.credentials_password_reset_redirect.instructions.title')}:
      </h3>
      <div className="prose text-sm text-left">
        <ul>
          <li>{translate('pages.identity.credentials_password_reset_redirect.instructions.steps.step1')}</li>
          <li>{translate('pages.identity.credentials_password_reset_redirect.instructions.steps.step2')}</li>
          <li>{translate('pages.identity.credentials_password_reset_redirect.instructions.steps.step3')}</li>
        </ul>

        <h3>{translate('pages.identity.credentials_password_reset_redirect.troubleshoot.question')}</h3>
        <p>{translate('pages.identity.credentials_password_reset_redirect.troubleshoot.answer')}</p>
      </div>
      <div className="text-center">
        <Link to="/" className="btn btn-secondary">
          {translate('pages.identity.credentials_password_reset_redirect.links.home')}
        </Link>
      </div>
    </FormPage>
  );
};
