import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import Loader from '../../../framework/components/loader/Loader.tsx';
import { GetOrganizationAction } from '../actions/getOrganization.ts';
import { ListAllMembershipsAction } from '../actions/listAllMemberships.ts';


export const OrganizationsManagePage: React.FC = () => {
  const { t: translate } = useTranslation();

  const { execute: listMemberships, lastSuccessResponse: memberships } = ListAllMembershipsAction();

  useEffect(() => listMemberships(), []);

  return (
    <FormPage title={translate('pages.organizations.manage.title')} align="top">
      <div className="space-y-4">
        {(memberships ?? [])
          .sort((a, b) => (a.isDefault ? -1 : b.isDefault ? 1 : 0))
          .map((membership) => (
            <OrganizationCard key={membership.organizationId} membership={membership} />
          ))}
      </div>

      {memberships?.length === 0 && (
        <div className="text-center py-8 text-gray-500">{translate('pages.organizations.manage.noOrganizations')}</div>
      )}
    </FormPage>
  );
};

const OrganizationCard: React.FC<{ membership: any }> = ({ membership }) => {
  const { t: translate } = useTranslation();
  const {
    execute: getOrganization,
    isExecuting: loadingOrganization,
    lastSuccessResponse: organization
  } = GetOrganizationAction(membership.organizationId);

  useEffect(() => getOrganization(), [membership.organizationId]);

  if (loadingOrganization) {
    return (
      <div className="p-4 border rounded-lg">
        <Loader message={translate('pages.organizations.manage.loader.title')} />
      </div>
    );
  }

  return (
    <div className={`p-2 rounded-lg ${membership.isDefault ? 'border-blue-500 border-3' : 'border-gray-200 border'}`}>
      <div className="flex items-center gap-4">
        <div className="relative">
          {organization?.avatarUrl ? (
            <img
              src={organization?.avatarUrl}
              alt={organization?.name}
              className="w-20 h-20 rounded-full object-cover"
            />
          ) : (
            <img
              src="/images/organization-icon.svg"
              alt={organization?.name}
              className="w-20 h-20 rounded-full object-cover"
            />
          )}
          {
            // @ts-ignore
            organization?.ownership === 'personal' && (
              <div
                className="absolute -bottom-1 -right-1 w-6 h-6 bg-gray-800 rounded-full flex items-center justify-center"
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

        <div className="flex-1">
          <div className="flex items-center gap-2">
            <h3 className="text-lg font-medium">{organization?.name}</h3>
          </div>
          <div className="space-y-1">
            <p className="text-sm text-gray-600">
              <span className="font-medium mr-2">
                {translate('pages.organizations.manage.form.fields.features.label')}:
              </span>
              {membership.features && membership.features.length > 0 ? (
                membership.features.map((feature: string, index: number) => (
                  <span
                    key={index}
                    className="inline-flex items-center mr-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200"
                  >
                    {formatFeatureName(feature)}
                  </span>
                ))
              ) : (
                <span className="text-gray-500 dark:text-gray-400 text-sm">
                  {translate('pages.organizations.manage.form.fields.features.empty')}
                </span>
              )}
            </p>
            <p className="text-sm text-gray-600">
              <span className="font-medium mr-2">
                {translate('pages.organizations.manage.form.fields.roles.label')}:
              </span>
              {membership.roles && membership.roles.length > 0 ? (
                membership.roles.map((role: string, index: number) => (
                  <span
                    key={index}
                    className="inline-flex items-center mr-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200"
                  >
                    {formatRoleName(role)}
                  </span>
                ))
              ) : (
                <span className="text-gray-500 dark:text-gray-400 text-sm">
                  {translate('pages.organizations.manage.form.fields.roles.empty')}
                </span>
              )}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

const formatRoleName = (role: string) => role.replace(/^tenant_/, '');

const formatFeatureName = (feature: string) => feature.replace(/^tenant_/, '').replace(/_features$/, '');
