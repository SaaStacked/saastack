import React, { useEffect, useRef, useState } from 'react';
import { UseFormReturn } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import z from 'zod';
import { ChangeOrganizationRequest, GetOrganizationResponse, Organization } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/apiHost1/emptyRequest.ts';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import ButtonUpload from '../../../framework/components/button/ButtonUpload.tsx';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormInput from '../../../framework/components/form/formInput/FormInput.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import Icon from '../../../framework/components/icon/Icon.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { useCurrentUser } from '../../../framework/providers/CurrentUserContext.tsx';
import { ChangeProfileAvatarRequest, UploadAvatarErrors } from '../../userProfiles/actions/changeProfileAvatar.ts';
import { ChangeOrganizationAction } from '../actions/changeOrganization.ts';
import { ChangeOrganizationAvatarAction, ChangeOrganizationAvatarRequest } from '../actions/changeOrganizationAvatar.ts';
import { DeleteOrganizationAvatarAction } from '../actions/deleteOrganizationAvatar.ts';
import { GetOrganizationAction } from '../actions/getOrganization.ts';


export const OrganizationEditPage: React.FC = () => {
  const { t: translate } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { refetch: refetchCurrentUser } = useCurrentUser();
  const getOrganization = GetOrganizationAction(id!);
  const organization = getOrganization.lastSuccessResponse
    ? getOrganization.lastSuccessResponse!
    : ({} as Organization);
  const changeOrganization = ChangeOrganizationAction(organization.id ?? '');
  const changeOrganizationAvatar = ChangeOrganizationAvatarAction(organization.id);
  const deleteOrganizationAvatar = DeleteOrganizationAvatarAction(organization.id);
  const [currentOrganization, setCurrentOrganization] = useState(organization);
  const getOrganizationTrigger = useRef<PageActionRef<EmptyRequest>>(null);
  const changeOrganizationAvatarTrigger = useRef<PageActionRef<ChangeOrganizationAvatarRequest>>(null);

  useEffect(() => getOrganizationTrigger.current?.execute(), []);

  const onOrganizationChange = (organization: Organization) => {
    setCurrentOrganization(organization);
    refetchCurrentUser();
  };

  return (
    <FormPage title={translate('pages.organizations.edit.title')} align="top">
      <PageAction
        id="get_organization"
        action={getOrganization}
        ref={getOrganizationTrigger}
        loadingMessage={translate('pages.organizations.edit.loader.title')}
      >
        <div className="w-full">
          <div className="flex flex-col items-center">
            <div className="relative">
              {currentOrganization.avatarUrl ? (
                <img
                  className="w-40 h-40 rounded-full object-cover"
                  src={currentOrganization.avatarUrl}
                  alt={currentOrganization.name}
                />
              ) : (
                <div className="w-40 h-40 bg-gray-200 rounded-full flex items-center justify-center">
                  <Icon symbol="company" size={100} color="gray-400" />
                </div>
              )}
              {
                organization?.ownership === 'personal' && (
                  <div
                    className="absolute -bottom-1 -right-1 w-8 h-8 bg-gray-800 rounded-full flex items-center justify-center"
                    title={translate('pages.organizations.manage.hints.ownership')}
                  >
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <rect x="6" y="10" width="12" height="8" rx="2" fill="white" />
                      <path d="M8 10V7a4 4 0 0 1 8 0v3" stroke="white" strokeWidth="2" strokeLinecap="round" />
                    </svg>
                  </div>
                )
              }
            </div>

            <div className="flex flex-row mt-2 space-x-2 items-center">
              <ButtonUpload
                className="p-2 rounded-full w-8 h-8"
                id="upload_avatar"
                onFileChange={(file) => {
                  if (file) {
                    changeOrganizationAvatarTrigger.current?.execute({ file } as ChangeProfileAvatarRequest);
                  }
                }}
                disabled={changeOrganizationAvatar.isExecuting}
              />
              <PageAction
                id="change_avatar"
                action={changeOrganizationAvatar}
                onSuccess={(params: {
                  requestData?: ChangeOrganizationAvatarRequest;
                  response: GetOrganizationResponse;
                }) => {
                  const updated = params.response.organization;
                  onOrganizationChange(updated);
                }}
                expectedErrorMessages={{
                  [UploadAvatarErrors.invalid_image]: translate(
                    'pages.profiles.manage.tabs.profile.errors.invalid_image'
                  )
                }}
                ref={changeOrganizationAvatarTrigger}
              />
              {currentOrganization.avatarUrl && (
                <ButtonAction
                  className="p-2 rounded-full w-8 h-8"
                  id="delete_avatar"
                  action={deleteOrganizationAvatar}
                  onSuccess={(params: { requestData?: EmptyRequest; response: GetOrganizationResponse }) => {
                    const updated = params.response.organization;
                    onOrganizationChange(updated);
                  }}
                  variant="danger"
                >
                  <Icon symbol="trash" size={16} color="white" />
                </ButtonAction>
              )}
            </div>

            <div className="mt-4 w-full">
              <FormAction
                action={changeOrganization}
                validationSchema={z.object({
                  name: z
                    .string()
                    .min(1, translate('pages.organizations.edit.form.fields.name.validation'))
                    .max(100, translate('pages.organizations.edit.form.fields.name.validation'))
                })}
                defaultValues={{ name: organization.name }}
                onSuccess={(params: {
                  requestData?: ChangeOrganizationRequest;
                  response: GetOrganizationResponse;
                  formMethods: UseFormReturn<any>;
                }) => {
                  const updated = params.response.organization;
                  onOrganizationChange(updated);
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
        </div>
      </PageAction>
      <div className="text-center">
        <Link to="/organizations">{translate('pages.organizations.edit.links.organizations')}</Link>
      </div>
    </FormPage>
  );
};
