import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';


export const AfterRegisterCredentials: React.FC = () => {
  const { t: translate } = useTranslation('common');
  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="prose">
        <div className="grid grid-cols-1 gap-4 items-left">
          <svg width="64" height="64" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle cx="12" cy="12" r="10" stroke="#28a745" strokeWidth="2" fill="none" />
            <path d="m9 12 2 2 4-4" stroke="#28a745" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
          <h1>{translate('pages.identity.after_register_credentials.title')}</h1>
        </div>

        <h2>{translate('pages.identity.after_register_credentials.confirmation_message.title')}</h2>
        <p>{translate('pages.identity.after_register_credentials.confirmation_message.message')}</p>

        <div className="email-instructions">
          <h3>{translate('pages.identity.after_register_credentials.instructions.title')}:</h3>
          <ol>
            <li>{translate('pages.identity.after_register_credentials.instructions.steps.step1')}</li>
            <li>{translate('pages.identity.after_register_credentials.instructions.steps.step2')}</li>
            <li>{translate('pages.identity.after_register_credentials.instructions.steps.step3')}</li>
          </ol>
        </div>

        <h3>{translate('pages.identity.after_register_credentials.troubleshoot.question')}</h3>
        <p>{translate('pages.identity.after_register_credentials.troubleshoot.answer')}</p>

        <Link to="/" className="btn btn-secondary">
          {translate('pages.identity.after_register_credentials.links.home')}
        </Link>
      </div>
    </div>
  );
};
