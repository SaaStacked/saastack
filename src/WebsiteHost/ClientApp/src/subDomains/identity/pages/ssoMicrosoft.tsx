import React from 'react';
import { useTranslation } from 'react-i18next';
import Card from '../../../framework/components/form/Card.tsx';


export const SsoMicrosoftPage: React.FC = () => {
  const { t: translate } = useTranslation();
  return (
    <Card>
      <h1 className="text-4xl font-bold text-center mb-16">{translate('pages.identity.sso_microsoft.title')}</h1>
    </Card>
  );
};
