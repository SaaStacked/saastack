import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import z from 'zod';
import Alert from '../../../framework/components/alert/Alert.tsx';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormInput from '../../../framework/components/form/formInput/FormInput.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import { CreateOrganizationAction } from '../actions/createOrganization.ts';


export const OrganizationNewPage: React.FC = () => {
  const { t: translate } = useTranslation();
  const [completed, setCompleted] = React.useState(false);
  const createOrganization = CreateOrganizationAction();

  return (
    <FormPage title={translate('pages.organizations.new.title')} align="top">
      {completed && (
        <Alert
          id="confirmation_message"
          type="warning"
          title={translate('pages.organizations.new.messages.confirmation.title')}
          message={translate('pages.organizations.new.messages.confirmation.message')}
        />
      )}
      <FormAction
        action={createOrganization}
        validationSchema={z.object({
          name: z
            .string()
            .min(1, translate('pages.organizations.edit.form.fields.name.validation'))
            .max(100, translate('pages.organizations.edit.form.fields.name.validation'))
        })}
        onSuccess={() => setCompleted(true)}
      >
        <FormInput
          id="name"
          name="name"
          label={translate('pages.organizations.edit.form.fields.name.label')}
          placeholder={translate('pages.organizations.edit.form.fields.name.placeholder')}
        />
        <FormSubmitButton label={translate('pages.organizations.edit.form.submit.label')} />
      </FormAction>
      <div className="text-center">
        <Link to="/organizations">{translate('pages.organizations.new.links.organizations')}</Link>
      </div>
    </FormPage>
  );
};
