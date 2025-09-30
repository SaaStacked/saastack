import React, { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ChangeDefaultOrganizationRequest, Organization, UpdateUserResponse } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/apiHost1/emptyRequest.ts';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import Icon from '../../../framework/components/icon/Icon.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import Tag from '../../../framework/components/tag/Tag.tsx';
import { useCurrentUser } from '../../../framework/providers/CurrentUserContext.tsx';
import { ChangeDefaultOrganizationAction } from '../../endUsers/actions/changeDefaultOrganization.ts';
import { ListAllMembershipsAction } from '../../endUsers/actions/listAllMemberships.ts';
import { GetOrganizationAction, OrganizationErrorCodes } from '../actions/getOrganization.ts';


export const OrganizationsManagePage: React.FC = () => {
  const { t: translate } = useTranslation();
  const { refetch: refetchCurrentUser } = useCurrentUser();
  const listAllMemberships = ListAllMembershipsAction();
  const listAllMembershipsTrigger = useRef<PageActionRef<EmptyRequest>>(null);
  const memberships = listAllMemberships.lastSuccessResponse!;

  useEffect(() => listAllMembershipsTrigger.current?.execute(), []);

  return (
    <FormPage title={translate('pages.organizations.manage.title')} align="top">
      <PageAction
        id="get_organization"
        action={listAllMemberships}
        ref={listAllMembershipsTrigger}
        loadingMessage={translate('pages.organizations.manage.loader.memberships')}
      >
        <div className="space-y-4">
          {(memberships ?? [])
            .sort((a, b) => (a.isDefault ? -1 : b.isDefault ? 1 : 0))
            .map((membership) => (
              <OrganizationCard
                key={membership.organizationId}
                membership={membership}
                onMembershipChange={() => {
                  refetchCurrentUser();
                  listAllMembershipsTrigger.current?.execute();
                }}
              />
            ))}
        </div>

        {memberships?.length === 0 && (
          <div className="text-center py-8 text-gray-500">
            {translate('pages.organizations.manage.noOrganizations')}
          </div>
        )}
      </PageAction>
      <div className="text-center">
        <Link to="/organizations/new">{translate('pages.organizations.manage.links.new')}</Link>
      </div>
    </FormPage>
  );
};

const OrganizationCard: React.FC<{
  membership: any;
  onMembershipChange: () => void;
}> = ({ membership, onMembershipChange }) => {
  const { t: translate } = useTranslation();
  const changeDefaultOrganization = ChangeDefaultOrganizationAction();
  const getOrganization = GetOrganizationAction(membership.organizationId);
  const getOrganizationTrigger = useRef<PageActionRef<EmptyRequest>>(null);
  const organization = getOrganization.lastSuccessResponse ?? ({} as Organization);

  useEffect(() => getOrganizationTrigger.current?.execute(), []);

  return (
    <>
      <PageAction
        id="get_organization"
        action={getOrganization}
        ref={getOrganizationTrigger}
        expectedErrorMessages={{
          [OrganizationErrorCodes.forbidden]: translate('pages.organizations.manage.errors.forbidden')
        }}
        loadingMessage={translate('pages.organizations.manage.loader.organization')}
      >
        <div
          className={`relative p-2 rounded-lg ${membership.isDefault ? 'border-accent border-3' : 'border-gray-200 border'}`}
        >
          {membership.isDefault && (
            <Tag
              className="absolute -top-3 left-4 text-xs"
              label={translate('pages.organizations.manage.labels.current')}
              color="accent"
            />
          )}
          <div className="flex items-center gap-4">
            <div className="relative">
              {organization?.avatarUrl ? (
                <img
                  className="w-20 h-20 rounded-full object-cover"
                  src={organization?.avatarUrl}
                  alt={organization?.name}
                />
              ) : (
                <Icon
                  className="w-20 h-20 rounded-full object-cover bg-gray-200"
                  symbol="company"
                  size={60}
                  color="gray-400"
                />
              )}
              {organization?.ownership === 'personal' && (
                <div
                  className="absolute -bottom-1 -right-1 w-8 h-8 bg-gray-800 rounded-full flex items-center justify-center"
                  title={translate('pages.organizations.manage.hints.ownership')}
                >
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <rect x="6" y="10" width="12" height="8" rx="2" fill="white" />
                    <path d="M8 10V7a4 4 0 0 1 8 0v3" stroke="white" strokeWidth="2" strokeLinecap="round" />
                  </svg>
                </div>
              )}
            </div>

            <div className="flex-1">
              <div className="flex items-center gap-2">
                <h3 className="text-lg font-medium">{organization?.name}</h3>
              </div>
              <div className="space-y-1">
                <p className="text-sm text-gray-600">
                  <span className="font-medium mr-2">
                    {translate('pages.organizations.manage.labels.roles.label')}:
                  </span>
                  {membership.roles && membership.roles.length > 0 ? (
                    membership.roles.map((role: string, index: number) => (
                      <Tag key={index} className="text-xs" label={formatRoleName(role)} color="sky" />
                    ))
                  ) : (
                    <Tag
                      className="text-xs"
                      label={translate('pages.organizations.manage.labels.roles.empty')}
                      color="gray"
                    />
                  )}
                </p>
                <p className="text-sm text-gray-600">
                  <span className="font-medium mr-2">
                    {translate('pages.organizations.manage.labels.features.label')}:
                  </span>
                  {membership.features && membership.features.length > 0 ? (
                    membership.features.map((feature: string, index: number) => (
                      <Tag key={index} className="text-xs" label={formatFeatureName(feature)} color="lime" />
                    ))
                  ) : (
                    <Tag
                      className="text-xs"
                      label={translate('pages.organizations.manage.labels.features.empty')}
                      color="gray"
                    />
                  )}
                </p>
              </div>
            </div>

            <div className="flex flex-col gap-2">
              <div className="flex items-center justify-center">
                <Link
                  to={`/organizations/${membership.organizationId}/edit`}
                  title={translate('pages.organizations.manage.hints.edit')}
                >
                  <Icon id="edit_icon" color="accent" symbol="edit" size={25} />
                </Link>
              </div>
              {!membership.isDefault && (
                <div className="flex items-center">
                  <ButtonAction
                    id="switch"
                    action={changeDefaultOrganization}
                    requestData={{ organizationId: membership.organizationId }}
                    onSuccess={(_params: {
                      requestData?: ChangeDefaultOrganizationRequest;
                      response: UpdateUserResponse;
                    }) => onMembershipChange()}
                    variant="ghost"
                    title={translate('pages.organizations.manage.hints.switch_default')}
                  >
                    <Icon id="switch_icon" color="accent" symbol="shuffle" size={25} />
                  </ButtonAction>
                </div>
              )}
            </div>
          </div>
        </div>
      </PageAction>
    </>
  );
};

const formatRoleName = (role: string) => role.replace(/^tenant_/, '');

const formatFeatureName = (feature: string) => feature.replace(/^tenant_/, '').replace(/_features$/, '');
