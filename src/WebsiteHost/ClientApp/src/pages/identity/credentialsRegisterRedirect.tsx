import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

export const CredentialsRegisterRedirect: React.FC = () => {
  const { t: translate } = useTranslation('common');
  return (
    <div className="container min-h-screen flex">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <h1 className="text-4xl font-bold text-center mb-16">
          {translate('pages.identity.credentials_register_redirect.title')}
        </h1>
        <h2 className="text-2xl font-bold text-center mb-8">
          {translate('pages.identity.credentials_register_redirect.confirmation_message.title')}
        </h2>
        <p>{translate('pages.identity.credentials_register_redirect.confirmation_message.message')}</p>
        <div className="flex justify-center">
          <svg width="128" height="128" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle cx="12" cy="12" r="10" stroke="#28a745" strokeWidth="2" fill="none" />
            <path d="m9 12 2 2 4-4" stroke="#28a745" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </div>
        <h3 className="text-2xl font-bold text-center mt-8">
          {translate('pages.identity.credentials_register_redirect.instructions.title')}:
        </h3>
        <div className="prose text-sm text-center">
          <ul>
            <li>{translate('pages.identity.credentials_register_redirect.instructions.steps.step1')}</li>
            <li>{translate('pages.identity.credentials_register_redirect.instructions.steps.step2')}</li>
            <li>{translate('pages.identity.credentials_register_redirect.instructions.steps.step3')}</li>
          </ul>

          <h3>{translate('pages.identity.credentials_register_redirect.troubleshoot.question')}</h3>
          <p>{translate('pages.identity.credentials_register_redirect.troubleshoot.answer')}</p>
        </div>
        <div className="text-center">
          <Link to="/" className="btn btn-secondary">
            {translate('pages.identity.credentials_register_redirect.links.home')}
          </Link>
        </div>
      </div>
    </div>
  );
};
