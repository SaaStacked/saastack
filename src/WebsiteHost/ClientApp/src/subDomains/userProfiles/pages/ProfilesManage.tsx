import React from 'react';
import { useTranslation } from 'react-i18next';
import FormPage from '../../../framework/components/form/FormPage.tsx';

export const ProfilesManagePage: React.FC = () => {
  const { t: translate } = useTranslation();
  return <FormPage title={translate('pages.profiles.manage.title')}>&nbsp;</FormPage>;
};
