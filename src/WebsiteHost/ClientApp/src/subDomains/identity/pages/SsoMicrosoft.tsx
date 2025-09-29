import React from 'react';
import { useTranslation } from 'react-i18next';
import FormPage from '../../../framework/components/form/FormPage.tsx';

export const SsoMicrosoftPage: React.FC = () => {
  const { t: translate } = useTranslation();
  return <FormPage title={translate('pages.identity.sso_microsoft.title')}>&nbsp;</FormPage>;
};
