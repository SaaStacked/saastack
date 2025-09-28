import React, { useEffect, useRef, useState } from 'react';
import { UseFormReturn } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import z from 'zod';
import { ChangeProfileAvatarResponse, ChangeProfileRequest, DeleteProfileAvatarResponse, GetProfileResponse, UserProfileForCaller } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/apiHost1/emptyRequest.ts';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import ButtonUpload from '../../../framework/components/button/ButtonUpload.tsx';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormInput from '../../../framework/components/form/formInput/FormInput.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSelect from '../../../framework/components/form/formSelect/FormSelect.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import { FormTabs } from '../../../framework/components/form/FormTabs.tsx';
import Icon from '../../../framework/components/icon/Icon.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { useCurrentUser } from '../../../framework/providers/CurrentUserContext.tsx';
import { countryTimezones } from '../../../framework/utils/browser.ts';
import { ChangeProfileAction } from '../actions/changeProfile.ts';
import { ChangeProfileAvatarAction, ChangeProfileAvatarRequest, UploadAvatarErrors } from '../actions/changeProfileAvatar.ts';
import { DeleteProfileAvatarAction } from '../actions/deleteProfileAvatar.ts';
import { GetProfileForCallerAction } from '../actions/getProfileForCaller.ts';


export const ProfilesManagePage: React.FC = () => {
  const { t: translate } = useTranslation();
  const { refetch: refetchCurrentUser } = useCurrentUser();
  const getProfile = GetProfileForCallerAction();
  const profile = getProfile.lastSuccessResponse!;
  const [currentProfile, setCurrentProfile] = useState(profile);
  const getProfileTrigger = useRef<PageActionRef<EmptyRequest>>(null);

  useEffect(() => getProfileTrigger.current?.execute(), []);

  const onProfileChange = (profile: UserProfileForCaller) => {
    setCurrentProfile(profile);
    refetchCurrentUser();
  };

  const tabs = [
    {
      id: 'account',
      label: translate('pages.profiles.manage.tabs.account.title'),
      content: <AccountTab initialProfile={profile} currentProfile={currentProfile} />
    },
    {
      id: 'profile',
      label: translate('pages.profiles.manage.tabs.profile.title'),
      content: <ProfileTab currentProfile={currentProfile} onProfileChange={onProfileChange} />
    }
  ];

  return (
    <FormPage title={translate('pages.profiles.manage.title')} align="top">
      <PageAction
        id="get_profile"
        action={getProfile}
        onSuccess={(params: { requestData?: EmptyRequest; response: UserProfileForCaller }) => {
          const updated = params.response;
          setCurrentProfile(updated);
        }}
        ref={getProfileTrigger}
        loadingMessage={translate('pages.profiles.manage.loader.title')}
      >
        <FormTabs tabs={tabs} defaultTab="account" storageKey="profiles.manage" />
      </PageAction>
    </FormPage>
  );
};

const AccountTab: React.FC<{
  initialProfile: UserProfileForCaller;
  currentProfile: UserProfileForCaller;
}> = ({ initialProfile, currentProfile }) => {
  const { t: translate } = useTranslation();
  return (
    <div className="space-y-6">
      <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="block text-sm font-medium text-gray-600 dark:text-gray-400 mb-1">
              {translate('pages.profiles.manage.tabs.account.form.fields.name.label')}
            </label>
            <p className="text-gray-900 dark:text-gray-100">
              {currentProfile.name?.firstName} {currentProfile.name?.lastName}
            </p>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-600 dark:text-gray-400 mb-1">
              {translate('pages.profiles.manage.tabs.account.form.fields.email_address.label')}
            </label>
            <p className="text-gray-900 dark:text-gray-100">{initialProfile.emailAddress}</p>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-600 dark:text-gray-400 mb-1">
              {translate('pages.profiles.manage.tabs.account.form.fields.roles.label')}
            </label>
            <div className="flex flex-wrap gap-2">
              {initialProfile.roles && initialProfile.roles.length > 0 ? (
                initialProfile.roles.map((role, index) => (
                  <span
                    key={index}
                    className="inline-flex items-center mr-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200"
                  >
                    {formatRoleName(role)}
                  </span>
                ))
              ) : (
                <span className="text-gray-500 dark:text-gray-400 text-sm">
                  {translate('pages.profiles.manage.tabs.account.form.fields.roles.empty')}
                </span>
              )}
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-600 dark:text-gray-400 mb-1">
              {translate('pages.profiles.manage.tabs.account.form.fields.features.label')}
            </label>
            <div className="flex flex-wrap gap-2">
              {initialProfile.features && initialProfile.features.length > 0 ? (
                initialProfile.features.map((feature, index) => (
                  <span
                    key={index}
                    className="inline-flex items-center mr-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200"
                  >
                    {formatFeatureName(feature)}
                  </span>
                ))
              ) : (
                <span className="text-gray-500 dark:text-gray-400 text-sm">
                  {translate('pages.profiles.manage.tabs.account.form.fields.features.empty')}
                </span>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

const ProfileTab: React.FC<{
  currentProfile: UserProfileForCaller;
  onProfileChange: (profile: UserProfileForCaller) => void;
}> = ({ currentProfile, onProfileChange }) => {
  const { t: translate } = useTranslation();
  const changeProfile = ChangeProfileAction(currentProfile.userId);
  const changeAvatar = ChangeProfileAvatarAction(currentProfile.userId);
  const deleteAvatar = DeleteProfileAvatarAction(currentProfile.userId);
  const timezoneOptions = Object.keys(countryTimezones).map((timezone) => ({
    label: timezone,
    value: timezone
  }));
  const localeOptions = [
    //EXTEND: Add more languages
    { label: translate('app.languages.en'), value: 'en' }
  ];
  const displayName = currentProfile?.displayName
    ? currentProfile?.displayName
    : currentProfile?.name?.firstName || '?';
  const avatarLetter = displayName.charAt(0);
  const changeAvatarTrigger = useRef<PageActionRef<ChangeProfileAvatarRequest>>(null);

  return (
    <>
      <div className="mb-6">
        <div className="flex flex-col items-center">
          <div className="w-40 h-40 rounded-full overflow-hidden bg-gray-200 dark:bg-gray-700  flex items-center justify-center">
            {currentProfile.avatarUrl ? (
              <img className="w-full h-full  object-cover" src={currentProfile.avatarUrl} alt={displayName} />
            ) : (
              <span className="text-gray-400 text-5xl leading-none">{avatarLetter}</span>
            )}
          </div>

          <div className="flex flex-row mt-2 space-x-2 items-center">
            <ButtonUpload
              className="p-2 rounded-full w-8 h-8"
              id="upload_avatar"
              onFileChange={(file) => {
                if (file) {
                  changeAvatarTrigger.current?.execute({ file } as ChangeProfileAvatarRequest);
                }
              }}
              disabled={changeAvatar.isExecuting}
            />
            <PageAction
              id="change_avatar"
              action={changeAvatar}
              onSuccess={(params: {
                requestData?: ChangeProfileAvatarRequest;
                response: ChangeProfileAvatarResponse;
              }) => {
                const updated = params.response.profile as UserProfileForCaller;
                onProfileChange(updated);
              }}
              expectedErrorMessages={{
                [UploadAvatarErrors.invalid_image]: translate('pages.profiles.manage.tabs.profile.errors.invalid_image')
              }}
              ref={changeAvatarTrigger}
            />
            {currentProfile.avatarUrl && (
              <ButtonAction
                className="p-2 rounded-full w-8 h-8"
                id="delete_avatar"
                action={deleteAvatar}
                onSuccess={(params: { requestData?: EmptyRequest; response: DeleteProfileAvatarResponse }) => {
                  const updated = params.response.profile as UserProfileForCaller;
                  onProfileChange(updated);
                }}
                variant="danger"
              >
                <Icon symbol="trash" size={16} color="white" />
              </ButtonAction>
            )}
          </div>
        </div>
      </div>

      <FormAction
        id="change_profile"
        action={changeProfile}
        validationSchema={z.object({
          displayName: z
            .string()
            .min(1, translate('pages.profiles.manage.tabs.profile.form.fields.display_name.validation'))
            .max(100, translate('pages.profiles.manage.tabs.profile.form.fields.display_name.validation')),
          locale: z.string().min(2, translate('pages.profiles.manage.tabs.profile.form.fields.locale.validation')),
          timezone: z.string().min(2, translate('pages.profiles.manage.tabs.profile.form.fields.timezone.validation'))
        })}
        defaultValues={{
          displayName: currentProfile.displayName || '',
          locale: currentProfile.locale || '',
          timezone: currentProfile.timezone || ''
        }}
        onSuccess={(params: {
          requestData?: ChangeProfileRequest;
          response: GetProfileResponse;
          formMethods: UseFormReturn<any>;
        }) => {
          const updated = params.response.profile as UserProfileForCaller;
          onProfileChange(updated);
          params.formMethods.reset({
            displayName: updated.displayName || '',
            locale: updated.locale || '',
            timezone: updated.timezone || ''
          });
          return;
        }}
      >
        <FormInput
          id="displayName"
          name="displayName"
          label={translate('pages.profiles.manage.tabs.profile.form.fields.display_name.label')}
          placeholder={translate('pages.profiles.manage.tabs.profile.form.fields.display_name.placeholder')}
        />
        <FormSelect
          id="locale"
          name="locale"
          label={translate('pages.profiles.manage.tabs.profile.form.fields.locale.label')}
          placeholder={translate('pages.profiles.manage.tabs.profile.form.fields.locale.placeholder')}
          options={localeOptions}
        />
        <FormSelect
          id="timezone"
          name="timezone"
          label={translate('pages.profiles.manage.tabs.profile.form.fields.timezone.label')}
          placeholder={translate('pages.profiles.manage.tabs.profile.form.fields.timezone.placeholder')}
          options={timezoneOptions}
        />
        <FormSubmitButton label={translate('pages.profiles.manage.tabs.profile.form.submit.label')} />
      </FormAction>
    </>
  );
};

const formatRoleName = (role: string) => role.replace(/^platform_/, '');

const formatFeatureName = (feature: string) => feature.replace(/^platform_/, '').replace(/_features$/, '');
