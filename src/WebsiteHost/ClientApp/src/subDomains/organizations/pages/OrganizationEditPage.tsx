import React, { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import z from 'zod';
import { ChangeOrganizationPatchResponse, Organization } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/apiHost1/emptyRequest.ts';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import ButtonUpload from '../../../framework/components/button/ButtonUpload.tsx';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormInput from '../../../framework/components/form/formInput/FormInput.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import Icon from '../../../framework/components/icon/Icon.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { ChangeProfileAvatarRequest, UploadAvatarErrors } from '../../userProfiles/actions/changeProfileAvatar.ts';
import { ChangeOrganizationAction } from '../actions/changeOrganization.ts';
import {
  ChangeOrganizationAvatarAction,
  ChangeOrganizationAvatarRequest
} from '../actions/changeOrganizationAvatar.ts';
import { DeleteOrganizationAvatarAction } from '../actions/deleteOrganizationAvatar.ts';
import { GetOrganizationAction } from '../actions/getOrganization.ts';

export const OrganizationEditPage: React.FC = () => {
  const { t: translate } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const getOrganization = GetOrganizationAction(id!);
  const organization = getOrganization.lastSuccessResponse
    ? getOrganization.lastSuccessResponse!
    : ({} as Organization);
  const changeOrganization = ChangeOrganizationAction(organization.id ?? '');
  const changeOrganizationAvatar = ChangeOrganizationAvatarAction(organization.id);
  const deleteOrganizationAvatar = DeleteOrganizationAvatarAction(organization.id);
  const [currentOrganization, setCurrentOrganization] = useState(organization);
  const getOrganizationRef = useRef<PageActionRef<EmptyRequest>>(null);
  const changeOrganizationAvatarRef = useRef<PageActionRef<ChangeOrganizationAvatarRequest>>(null);

  useEffect(() => getOrganizationRef.current?.execute(), []);

  return (
    <PageAction
      id="get_organization"
      action={getOrganization}
      ref={getOrganizationRef}
      loadingMessage={translate('pages.organizations.edit.loader.title')}
    >
      <FormPage title={translate('pages.organizations.edit.title')} align="top">
        <div className="max-w-2xl mx-auto space-y-8">
          <div className="bg-white dark:bg-gray-800 p-6 rounded-lg border">
            <div className="flex items-center space-x-6">
              <div className="relative">
                {currentOrganization.avatarUrl ? (
                  <img
                    className="w-full h-full  object-cover"
                    src={currentOrganization.avatarUrl}
                    alt={currentOrganization.name}
                  />
                ) : (
                  <div className="w-24 h-24 bg-gray-200 rounded-full flex items-center justify-center">
                    <Icon symbol="company" size={40} color="gray-400" />
                  </div>
                )}
              </div>

              <div className="flex flex-row mt-2 space-x-2 items-center">
                <ButtonUpload
                  className="p-2 rounded-full w-8 h-8"
                  id="upload_avatar"
                  onFileChange={(file) => {
                    if (file) {
                      changeOrganizationAvatarRef.current?.execute({ file } as ChangeProfileAvatarRequest);
                    }
                  }}
                  disabled={changeOrganizationAvatar.isExecuting}
                />
                <PageAction
                  id="change_avatar"
                  action={changeOrganizationAvatar as any}
                  onSuccess={(params) => {
                    const updated = (params.response as ChangeOrganizationPatchResponse).organization;
                    setCurrentOrganization(updated);
                  }}
                  expectedErrorMessages={{
                    [UploadAvatarErrors.invalid_image]: translate(
                      'pages.profiles.manage.tabs.profile.errors.invalid_image'
                    )
                  }}
                  ref={changeOrganizationAvatarRef as any}
                />
                {currentOrganization.avatarUrl && (
                  <ButtonAction
                    className="p-2 rounded-full w-8 h-8"
                    id="delete_avatar"
                    action={deleteOrganizationAvatar}
                    onSuccess={(params) => {
                      const updated = params.response.organization as Organization;
                      setCurrentOrganization(updated);
                    }}
                    variant="danger"
                  >
                    <Icon symbol="trash" size={16} color="white" />
                  </ButtonAction>
                )}
              </div>
            </div>
          </div>

          <div className="bg-white dark:bg-gray-800 p-6 rounded-lg border">
            <FormAction
              action={changeOrganization}
              validationSchema={z.object({
                name: z
                  .string()
                  .min(1, translate('pages.organizations.edit.form.fields.name.validation'))
                  .max(100, translate('pages.organizations.edit.form.fields.name.validation'))
              })}
              defaultValues={{ name: organization.name }}
              onSuccess={(params) => {
                const updated = params.response.organization as Organization;
                setCurrentOrganization(updated);
                params.formMethods.reset({
                  name: updated.name || ''
                });
                return;
              }}
            >
              <FormInput
                id="name"
                name="name"
                label={translate('pages.organizations.edit.form.fields.name.label')}
                placeholder={translate('pages.organizations.edit.form.fields.name.placeholder')}
              />
              <FormSubmitButton label={translate('pages.organizations.edit.form.submit.label')} />
            </FormAction>
          </div>
        </div>
      </FormPage>
    </PageAction>
  );
};
