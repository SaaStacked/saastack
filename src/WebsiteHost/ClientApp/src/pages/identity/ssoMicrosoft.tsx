import React from 'react';
import { useTranslation } from 'react-i18next';

export const SsoMicrosoftPage: React.FC = () => {
  const { t: translate } = useTranslation('common');
  return (
    <div className="login-page">
      <div className="login-container">
        <h1>{translate('pages.identity.sso_microsoft.title')}</h1>
      </div>
    </div>
  );
};
