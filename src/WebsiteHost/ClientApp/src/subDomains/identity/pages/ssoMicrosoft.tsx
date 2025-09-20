import React from 'react';
import { useTranslation } from 'react-i18next';


export const SsoMicrosoftPage: React.FC = () => {
  const { t: translate } = useTranslation();
  return (
    <div className="container min-h-screen flex items-center justify-center">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <h1 className="text-4xl font-bold text-center mb-16">{translate('pages.identity.sso_microsoft.title')}</h1>
      </div>
    </div>
  );
};
