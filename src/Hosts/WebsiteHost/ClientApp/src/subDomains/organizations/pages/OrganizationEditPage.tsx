import React, { useEffect, useRef, useState } from 'react';
import { UseFormReturn } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import z from 'zod';
import {
  AssignRolesToOrganizationRequest,
  ChangeOrganizationRequest,
  GetOrganizationResponse,
  Organization,
  OrganizationMember,
  OrganizationOwnership,
  UnassignRolesFromOrganizationRequest,
  UserProfileClassification,
  UserProfileForCaller,
  type GetOrganizationData
} from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/EmptyRequest.ts';
import Button from '../../../framework/components/button/Button.tsx';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import ButtonUpload from '../../../framework/components/button/ButtonUpload.tsx';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormInput from '../../../framework/components/form/formInput/FormInput.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import Icon from '../../../framework/components/icon/Icon.tsx';
import { Tabs } from '../../../framework/components/navigation/Tabs.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import Select from '../../../framework/components/select/Select.tsx';
import Tag from '../../../framework/components/tag/Tag.tsx';
import { RoutePaths } from '../../../framework/constants.ts';
import { useCurrentUser } from '../../../framework/providers/CurrentUserContext.tsx';
import { UploadAvatarErrors } from '../../userProfiles/actions/changeProfileAvatar.ts';
import {
  AssignRolesToOrganizationAction,
  AssignRolesToOrganizationErrorCodes
} from '../actions/assignRolesToOrganization.ts';
import { ChangeOrganizationAction } from '../actions/changeOrganization.ts';
import {
  ChangeOrganizationAvatarAction,
  ChangeOrganizationAvatarRequest
} from '../actions/changeOrganizationAvatar.ts';
import { DeleteOrganizationAvatarAction } from '../actions/deleteOrganizationAvatar.ts';
import { GetOrganizationAction, OrganizationErrorCodes } from '../actions/getOrganization.ts';
import {
  InviteMemberToOrganizationAction,
  InviteMemberToOrganizationErrorCodes
} from '../actions/inviteMemberToOrganization.ts';
import {
  ListMembersForOrganizationAction,
  ListMembersForOrganizationErrorCodes
} from '../actions/listMembersForOrganization.ts';
import { UnAssignRolesFromOrganizationAction } from '../actions/unAssignRolesFromOrganization.ts';
import { UnInviteMemberFromOrganizationAction } from '../actions/unInviteMemberFromOrganization.ts';
import { formatRoleName, TenantRoles } from './Organizations.ts';

export const OrganizationEditPage: React.FC = () => {
  const { t: translate } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { refetch: refetchCurrentUser } = useCurrentUser();
  const getOrganization = GetOrganizationAction();
  const organization = getOrganization.lastSuccessResponse
    ? getOrganization.lastSuccessResponse!
    : ({} as Organization);
  const [updatedOrganization, setUpdatedOrganization] = useState(organization);
  const getOrganizationTrigger = useRef<PageActionRef<EmptyRequest>>(null);

  const onOrganizationChange = (updated: Organization) => {
    setUpdatedOrganization(updated);
    refetchCurrentUser();
  };

  useEffect(() => getOrganizationTrigger.current?.execute({ path: { Id: id! } } as GetOrganizationData), [id]);

  useEffect(() => {
    if (getOrganization.lastSuccessResponse) {
      setUpdatedOrganization(getOrganization.lastSuccessResponse);
    }
  }, [getOrganization.lastSuccessResponse]);

  return (
    <FormPage title={translate('pages.organizations.edit.title', { name: updatedOrganization.name })} align="top">
      {' '}
      <PageAction
        id="get_organization"
        action={getOrganization}
        ref={getOrganizationTrigger}
        expectedErrorMessages={{
          [OrganizationErrorCodes.forbidden]: translate('pages.organizations.edit.errors.forbidden')
        }}
        loadingMessage={translate('pages.organizations.edit.loader.title')}
      >
        <Tabs
          defaultTab="details"
          tabs={[
            {
              id: 'details',
              label: translate('pages.organizations.edit.tabs.details.title'),
              content: (
                <DetailsTab
                  initialOrganization={organization}
                  updatedOrganization={updatedOrganization}
                  onOrganizationChange={onOrganizationChange}
                />
              )
            },
            {
              id: 'members',
              label: translate('pages.organizations.edit.tabs.members.title'),
              content: <MembersTab currentOrganization={organization} />
            }
          ]}
        />
      </PageAction>
      <div className="text-center">
        <Link to={RoutePaths.Organizations}>{translate('pages.organizations.edit.links.organizations')}</Link>
      </div>
    </FormPage>
  );
};

const DetailsTab: React.FC<{
  initialOrganization: Organization;
  updatedOrganization: Organization;
  onOrganizationChange: (organization: Organization) => void;
}> = ({ initialOrganization, updatedOrganization, onOrganizationChange }) => {
  const { t: translate } = useTranslation();
  const changeOrganization = ChangeOrganizationAction(initialOrganization.id ?? '');
  const changeOrganizationAvatar = ChangeOrganizationAvatarAction(initialOrganization.id);
  const deleteOrganizationAvatar = DeleteOrganizationAvatarAction(initialOrganization.id);
  const changeOrganizationAvatarTrigger = useRef<PageActionRef<ChangeOrganizationAvatarRequest>>(null);

  const isPersonal = initialOrganization?.ownership === OrganizationOwnership.PERSONAL;

  return (
    <div className="w-full">
      <div className="flex flex-col items-center">
        <div className="relative">
          {updatedOrganization.avatarUrl ? (
            <img
              className="w-40 h-40 rounded-full object-cover"
              src={updatedOrganization.avatarUrl}
              alt={updatedOrganization.name}
            />
          ) : (
            <div className="w-40 h-40 bg-neutral-200 rounded-full flex items-center justify-center">
              <Icon symbol="company" size={100} color="neutral-400" />
            </div>
          )}
          {isPersonal && (
            <div
              className="absolute -bottom-1 -right-1 w-8 h-8 bg-neutral-800 rounded-full flex items-center justify-center"
              title={translate('pages.organizations.manage.hints.ownership')}
            >
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <rect x="6" y="10" width="12" height="8" rx="2" fill="white" />
                <path d="M8 10V7a4 4 0 0 1 8 0v3" stroke="white" strokeWidth="2" strokeLinecap="round" />
              </svg>
            </div>
          )}
        </div>

        <div className="flex flex-row mt-2 space-x-2 items-center">
          <ButtonUpload
            className="p-2 rounded-full w-8 h-8"
            id="upload_avatar"
            onFileChange={(file) => {
              if (file) {
                changeOrganizationAvatarTrigger.current?.execute({ file });
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
              [UploadAvatarErrors.invalid_image]: translate('pages.profiles.manage.tabs.profile.errors.invalid_image')
            }}
            ref={changeOrganizationAvatarTrigger}
          />
          {updatedOrganization.avatarUrl && (
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
                .min(1, translate('pages.organizations.edit.tabs.details.form.fields.name.validation'))
                .max(100, translate('pages.organizations.edit.tabs.details.form.fields.name.validation'))
            })}
            defaultValues={{ name: initialOrganization.name }}
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
              label={translate('pages.organizations.edit.tabs.details.form.fields.name.label')}
              placeholder={translate('pages.organizations.edit.tabs.details.form.fields.name.placeholder')}
            />
            <FormSubmitButton label={translate('pages.organizations.edit.tabs.details.form.submit.label')} />
          </FormAction>
        </div>
      </div>
    </div>
  );
};

const MembersTab: React.FC<{
  currentOrganization: Organization;
}> = ({ currentOrganization }) => {
  const { t: translate } = useTranslation();
  const { profile: currentUser } = useCurrentUser();
  const listMembers = ListMembersForOrganizationAction(currentOrganization.id);
  const listMembersTrigger = useRef<PageActionRef<EmptyRequest>>(null);
  const members = listMembers.lastSuccessResponse ?? [];

  const isPersonal = currentOrganization?.ownership === OrganizationOwnership.PERSONAL;

  useEffect(() => listMembersTrigger.current?.execute(), []);

  const refetchMembers = () => listMembersTrigger.current?.execute();

  return (
    <PageAction
      id="list_memberships"
      action={listMembers}
      ref={listMembersTrigger}
      expectedErrorMessages={{
        [ListMembersForOrganizationErrorCodes.not_member]: translate(
          'pages.organizations.edit.tabs.members.errors.list.not_member'
        )
      }}
      loadingMessage={translate('pages.organizations.edit.tabs.members.loader')}
    >
      <div className="space-y-6">
        {!isPersonal && <InviteGuest currentOrganization={currentOrganization} onMemberChange={refetchMembers} />}
        <div>
          {members.length === 0 ? (
            <div className="text-center py-8 text-neutral-500">
              {translate('pages.organizations.edit.tabs.members.empty')}
            </div>
          ) : (
            <div className="space-y-3">
              {members.map((member) => (
                <MemberCard
                  currentUser={currentUser}
                  key={member.id}
                  member={member}
                  currentOrganization={currentOrganization}
                  onMemberChange={refetchMembers}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </PageAction>
  );
};

const MemberCard: React.FC<{
  currentUser: UserProfileForCaller;
  member: OrganizationMember;
  currentOrganization: Organization;
  onMemberChange: () => void;
}> = ({ currentUser, member, currentOrganization, onMemberChange }) => {
  const { t: translate } = useTranslation();
  const unInviteMember = UnInviteMemberFromOrganizationAction(currentOrganization.id, member.userId);

  const isOwner = member.isOwner;
  const isSelf = member.userId === currentUser.userId;
  const isPersonal = currentOrganization?.ownership === OrganizationOwnership.PERSONAL;
  const isGuestUser = !member.isRegistered;
  const isPerson = member.classification === UserProfileClassification.PERSON;

  return (
    <div
      className={`relative p-2 rounded-lg ${isSelf ? 'border-brand-primary-500 border-3' : isGuestUser ? 'border-neutral-400 border-3' : 'border-neutral-200 border'}`}
    >
      <div className="flex items-center gap-4">
        <div className="flex-1">
          <div className="flex items-center gap-2 mt-2 mb-1">
            <Icon id="member_icon" symbol={isPerson ? 'user' : 'robot'} size={24} color="neutral-600" />
            <p className="font-medium">
              {member.name?.firstName} {member.name?.lastName}
            </p>
            {isSelf && (
              <Tag
                className="absolute -top-3 left-4 text-xs"
                label={translate('pages.organizations.edit.tabs.members.labels.self')}
                color="brand-primary"
              />
            )}
            {isGuestUser && (
              <Tag
                className="absolute -top-3 left-4 text-xs"
                label={translate('pages.organizations.edit.tabs.members.labels.unregistered')}
                color="neutral"
              />
            )}
          </div>
          {member.emailAddress && (
            <div className="flex items-center gap-2 flex-wrap text-xs">
              <label className="font-medium text-neutral-600 dark:text-neutral-400">
                {translate('pages.organizations.edit.tabs.members.labels.email_address')}:
              </label>
              <p className="text-neutral-600">{member.emailAddress}</p>
            </div>
          )}
          <div className="flex items-center gap-2 flex-wrap text-xs">
            <label className="font-medium text-neutral-600 dark:text-neutral-400">
              {translate('pages.organizations.edit.tabs.members.labels.roles')}:
            </label>
            <div className="flex flex-wrap gap-1">
              {member.roles.map((role, index) => (
                <Tag className="text-xs" key={index} label={formatRoleName(translate, role)} color="sky" />
              ))}
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-center">
            {!isSelf && !isPersonal && (
              <ChangeRoleForm
                currentOrganization={currentOrganization}
                member={member}
                onMemberChange={onMemberChange}
              />
            )}
          </div>
          <div className="flex items-center justify-center">
            {!isOwner && !isSelf && !isPersonal && (
              <>
                <ButtonAction
                  id={`remove_${member.id}`}
                  action={unInviteMember}
                  expectedErrorMessages={{
                    [InviteMemberToOrganizationErrorCodes.personal_organization]: translate(
                      'pages.organizations.edit.tabs.members.errors.uninvite.personal_organization'
                    ),
                    [InviteMemberToOrganizationErrorCodes.not_owner]: translate(
                      'pages.organizations.edit.tabs.members.errors.uninvite.not_owner'
                    )
                  }}
                  onSuccess={onMemberChange}
                  variant="danger"
                  title={translate('pages.organizations.edit.tabs.members.hints.uninvite')}
                >
                  <Icon symbol="trash" size={16} color="white" />
                </ButtonAction>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

const ChangeRoleForm: React.FC<{
  currentOrganization: Organization;
  member: OrganizationMember;
  onMemberChange: () => void;
}> = ({ currentOrganization, member, onMemberChange }) => {
  const { t: translate } = useTranslation();
  const assignRoles = AssignRolesToOrganizationAction(currentOrganization.id);
  const unAssignRoles = UnAssignRolesFromOrganizationAction(currentOrganization.id);
  const assignRolesTrigger = useRef<PageActionRef<AssignRolesToOrganizationRequest>>(null);
  const unAssignRolesTrigger = useRef<PageActionRef<UnassignRolesFromOrganizationRequest>>(null);

  const isOwner = member.roles.includes(TenantRoles.Owner);
  const isBillingAdmin = member.roles.includes(TenantRoles.BillingAdmin);

  const allRoleOptions = [
    { value: 'none', label: translate('pages.organizations.edit.tabs.members.role_form.fields.role.placeholder') },
    { value: TenantRoles.Owner, label: formatRoleName(translate, TenantRoles.Owner) },
    { value: TenantRoles.BillingAdmin, label: formatRoleName(translate, TenantRoles.BillingAdmin) },
    { value: TenantRoles.Member, label: formatRoleName(translate, TenantRoles.Member) }
  ];

  const roleOptions = allRoleOptions.filter((option) => {
    if (isOwner && option.value === TenantRoles.Member) {
      return true;
    }

    if (isBillingAdmin && option.value === TenantRoles.Owner) {
      return true;
    }

    return !member.roles.includes(option.value);
  });

  const handleRoleChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const selectedRole = event.target.value;
    if (selectedRole === 'none') {
      return;
    }

    switch (selectedRole) {
      case TenantRoles.Owner: {
        if (isBillingAdmin) {
          unAssignRolesTrigger.current?.execute({ userId: member.userId, roles: [TenantRoles.BillingAdmin] });
        } else {
          assignRolesTrigger.current?.execute({ userId: member.userId, roles: [TenantRoles.Owner] });
        }
        break;
      }
      case TenantRoles.BillingAdmin: {
        assignRolesTrigger.current?.execute({ userId: member.userId, roles: [TenantRoles.BillingAdmin] });
        break;
      }
      case TenantRoles.Member: {
        // Removing owner also removes billing admin, by default
        unAssignRolesTrigger.current?.execute({
          userId: member.userId,
          roles: [TenantRoles.Owner]
        });
        break;
      }
      default: {
        return;
      }
    }
  };

  return (
    <>
      <Select
        id={`role_${member.id}`}
        options={roleOptions}
        onChange={handleRoleChange}
        size="sm"
        label={translate('pages.organizations.edit.tabs.members.role_form.fields.role.label')}
        disabled={assignRoles.isExecuting}
      />
      <PageAction
        id="assign_roles"
        action={assignRoles}
        ref={assignRolesTrigger}
        expectedErrorMessages={{
          [AssignRolesToOrganizationErrorCodes.not_member]: translate(
            'pages.organizations.edit.tabs.members.errors.assign_roles.not_member'
          ),
          [AssignRolesToOrganizationErrorCodes.not_owner]: translate(
            'pages.organizations.edit.tabs.members.errors.assign_roles.not_owner'
          )
        }}
        onSuccess={onMemberChange}
      />{' '}
      <PageAction
        id="unassign_roles"
        action={unAssignRoles}
        ref={unAssignRolesTrigger}
        expectedErrorMessages={{
          [AssignRolesToOrganizationErrorCodes.not_member]: translate(
            'pages.organizations.edit.tabs.members.errors.unassign_roles.not_member'
          ),
          [AssignRolesToOrganizationErrorCodes.not_owner]: translate(
            'pages.organizations.edit.tabs.members.errors.unassign_roles.not_owner'
          )
        }}
        onSuccess={onMemberChange}
      />
    </>
  );
};

const InviteGuest: React.FC<{
  currentOrganization: Organization;
  onMemberChange: () => void;
}> = ({ currentOrganization, onMemberChange }) => {
  const { t: translate } = useTranslation();
  const inviteMember = InviteMemberToOrganizationAction(currentOrganization.id);
  const [showInviteForm, setShowInviteForm] = useState(false);

  return (
    <>
      <div className="text-right">
        <Button
          id="refresh"
          variant={'outline'}
          title={translate('pages.organizations.edit.tabs.members.hints.refresh')}
          onClick={onMemberChange}
        >
          <Icon symbol="repeat" size={16} color="brand-secondary" />
        </Button>
      </div>
      {!showInviteForm ? (
        <div className="text-right">
          <Button
            id="invite_toggle"
            variant={'brand-primary'}
            label={translate('pages.organizations.edit.tabs.members.invite_form.toggle.show')}
            onClick={() => setShowInviteForm(true)}
          />
        </div>
      ) : (
        <FormAction
          id="invite"
          className="border rounded-lg border-neutral-200"
          action={inviteMember}
          expectedErrorMessages={{
            [InviteMemberToOrganizationErrorCodes.personal_organization]: translate(
              'pages.organizations.edit.tabs.members.errors.invite.personal_organization'
            ),
            [InviteMemberToOrganizationErrorCodes.not_owner]: translate(
              'pages.organizations.edit.tabs.members.errors.invite.not_owner'
            )
          }}
          onSuccess={() => {
            onMemberChange();
            setShowInviteForm(false);
          }}
          validationSchema={z.object({
            email: z.email(translate('pages.organizations.edit.tabs.members.invite_form.fields.email.validation'))
          })}
        >
          <FormInput
            id="email"
            name="email"
            type="email"
            label={translate('pages.organizations.edit.tabs.members.invite_form.fields.email.label')}
            placeholder={translate('pages.organizations.edit.tabs.members.invite_form.fields.email.placeholder')}
          />
          <div className="flex gap-2 justify-end -mt-4">
            <FormSubmitButton label={translate('pages.organizations.edit.tabs.members.invite_form.submit.label')} />
            <div className="flex mt-4">
              <Button
                id="invite_cancel"
                size={'sm'}
                variant={'brand-secondary'}
                label={translate('pages.organizations.edit.tabs.members.invite_form.toggle.cancel')}
                onClick={() => setShowInviteForm(false)}
              />
            </div>
          </div>
        </FormAction>
      )}
    </>
  );
};
