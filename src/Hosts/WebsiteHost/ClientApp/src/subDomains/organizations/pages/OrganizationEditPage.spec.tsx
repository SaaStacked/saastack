import { fireEvent, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ActionResult } from '../../../framework/actions/Actions';
import { ChangeOrganizationPatchResponse, ChangeOrganizationRequest, OrganizationOwnership, UserProfileClassification } from '../../../framework/api/apiHost1';
import { renderWithTestingProviders } from '../../../framework/testing/TestingProviders';
import { OrganizationEditPage } from './OrganizationEditPage';
import { TenantRoles } from './Organizations';


// Mock data
const mockOrganization = {
  id: 'anorganizationid1',
  name: 'anorganizationname',
  ownership: OrganizationOwnership.SHARED,
  avatarUrl: null
};

const mockCurrentUser = {
  userId: 'auserid1',
  profile: {
    userId: 'auserid1',
    emailAddress: 'auser1@company.com',
    name: { firstName: 'afirstname', lastName: 'alastname' },
    memberships: []
  },
  organization: mockOrganization,
  isSuccess: true,
  isExecuting: false,
  isAuthenticated: true,
  refetch: vi.fn()
};

const mockMembers = [
  {
    id: 'amemberid1',
    userId: 'auserid1',
    name: { firstName: 'afirstname1', lastName: 'alastname1' },
    emailAddress: 'auser1@company.com',
    roles: [TenantRoles.Owner],
    isOwner: true,
    isRegistered: true,
    classification: UserProfileClassification.PERSON
  },
  {
    id: 'amemberid2',
    userId: 'auserid2',
    name: { firstName: 'afirstname2', lastName: 'alastname2' },
    emailAddress: 'auser2@company.com',
    roles: [TenantRoles.Member],
    isOwner: false,
    isRegistered: true,
    classification: UserProfileClassification.PERSON
  }
];

const mockGetOrganizationAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: true,
  lastSuccessResponse: mockOrganization,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockListMembersAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: true,
  lastSuccessResponse: mockMembers,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockChangeOrganizationAction: ActionResult<ChangeOrganizationRequest, any, ChangeOrganizationPatchResponse> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockChangeOrganizationAvatarAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockDeleteOrganizationAvatarAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockInviteMemberAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockAssignRolesAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockUnAssignRolesAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockUnInviteMemberAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useParams: () => ({ id: 'anorganizationid1' })
  };
});

vi.mock('../../../framework/providers/CurrentUserContext', async () => {
  const actual = await vi.importActual('../../../framework/providers/CurrentUserContext');
  return {
    ...actual,
    useCurrentUser: () => mockCurrentUser,
    CurrentUserProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>
  };
});

vi.mock('../actions/getOrganization', () => ({
  GetOrganizationAction: () => mockGetOrganizationAction,
  OrganizationErrorCodes: {
    forbidden: 'forbidden'
  }
}));

vi.mock('../actions/listMembersForOrganization', () => ({
  ListMembersForOrganizationAction: () => mockListMembersAction,
  ListMembersForOrganizationErrorCodes: {
    forbidden: 'forbidden'
  }
}));

vi.mock('../actions/changeOrganization', () => ({
  ChangeOrganizationAction: () => mockChangeOrganizationAction
}));

vi.mock('../actions/changeOrganizationAvatar', () => ({
  ChangeOrganizationAvatarAction: () => mockChangeOrganizationAvatarAction
}));

vi.mock('../actions/deleteOrganizationAvatar', () => ({
  DeleteOrganizationAvatarAction: () => mockDeleteOrganizationAvatarAction
}));

vi.mock('../actions/inviteMemberToOrganization', () => ({
  InviteMemberToOrganizationAction: () => mockInviteMemberAction,
  InviteMemberToOrganizationErrorCodes: {
    forbidden: 'forbidden',
    personal_organization_limit_reached: 'personal_organization_limit_reached'
  }
}));

vi.mock('../actions/assignRolesToOrganization', () => ({
  AssignRolesToOrganizationAction: () => mockAssignRolesAction,
  AssignRolesToOrganizationErrorCodes: {
    forbidden: 'forbidden'
  }
}));

vi.mock('../actions/unAssignRolesFromOrganization', () => ({
  UnAssignRolesFromOrganizationAction: () => mockUnAssignRolesAction
}));

vi.mock('../actions/unInviteMemberFromOrganization', () => ({
  UnInviteMemberFromOrganizationAction: () => mockUnInviteMemberAction
}));

describe('OrganizationEditPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockGetOrganizationAction.isSuccess = true;
    mockGetOrganizationAction.lastSuccessResponse = mockOrganization;
    mockListMembersAction.isSuccess = true;
    mockListMembersAction.lastSuccessResponse = mockMembers;
  });

  it('renders page title', () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);

    expect(screen.getByText('pages.organizations.edit.title')).toBeInTheDocument();
  });

  it('renders all tabs', () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);

    expect(screen.getByText('pages.organizations.edit.tabs.details.title')).toBeInTheDocument();
    expect(screen.getByText('pages.organizations.edit.tabs.members.title')).toBeInTheDocument();
  });
});

describe('Details Tab', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockGetOrganizationAction.isSuccess = true;
    mockGetOrganizationAction.lastSuccessResponse = mockOrganization;
    mockListMembersAction.isSuccess = true;
    mockListMembersAction.lastSuccessResponse = mockMembers;
  });

  it('renders organization name in details tab', () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    expect(screen.getByText('pages.organizations.edit.tabs.details.title')).toBeInTheDocument();

    expect(screen.getByDisplayValue('anorganizationname')).toBeInTheDocument();
  });

  it('when change name, updates organization', async () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    expect(screen.getByText('pages.organizations.edit.tabs.details.title')).toBeInTheDocument();

    const nameInput = screen.getByTestId('name_form_input_input');
    fireEvent.change(nameInput, { target: { value: 'anewname' } });
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.details.form.submit.label'));

    await waitFor(() => expect(mockChangeOrganizationAction.execute).toHaveBeenCalled());
  });
});

describe('Members Tab', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockGetOrganizationAction.isSuccess = true;
    mockGetOrganizationAction.lastSuccessResponse = mockOrganization;
    mockListMembersAction.isSuccess = true;
    mockListMembersAction.lastSuccessResponse = mockMembers;
  });

  it('when click members tab, displays list of members', async () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);

    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.title'));

    await waitFor(() => {
      expect(screen.getByText('afirstname1 alastname1')).toBeInTheDocument();
      expect(screen.getByText('afirstname2 alastname2')).toBeInTheDocument();
    });
  });

  it('when personal organization, does not show invite section', () => {
    mockGetOrganizationAction.lastSuccessResponse = { ...mockOrganization, ownership: OrganizationOwnership.PERSONAL };

    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.title'));

    expect(screen.queryByText('pages.organizations.edit.tabs.members.invite_form.toggle.show')).not.toBeInTheDocument();
  });
});

describe('Member card', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockGetOrganizationAction.isSuccess = true;
    mockGetOrganizationAction.lastSuccessResponse = mockOrganization;
    mockListMembersAction.isSuccess = true;
    mockListMembersAction.lastSuccessResponse = mockMembers;
  });

  it('displays member information', async () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.title'));

    await waitFor(() => {
      expect(screen.getByText('afirstname1 alastname1')).toBeInTheDocument();
      expect(screen.getByText('auser1@company.com')).toBeInTheDocument();
    });
  });

  it('when current user, shows self tag', async () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.title'));

    await waitFor(() =>
      expect(screen.getByText('pages.organizations.edit.tabs.members.labels.self')).toBeInTheDocument()
    );
  });

  it('when person classification, shows person icon', async () => {
    mockListMembersAction.lastSuccessResponse = [
      {
        ...mockMembers[0],
        classification: UserProfileClassification.PERSON
      }
    ];

    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.title'));

    await waitFor(() => expect(screen.getByText('afirstname1 alastname1')).toBeInTheDocument());

    expect(screen.getByTestId('member_icon_icon_symbol_user')).toBeInTheDocument();
  });

  it('when machine classification, shows machine icon', async () => {
    mockListMembersAction.lastSuccessResponse = [
      {
        ...mockMembers[0],
        classification: UserProfileClassification.MACHINE
      }
    ];

    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.title'));

    await waitFor(() => expect(screen.getByText('afirstname1 alastname1')).toBeInTheDocument());

    expect(screen.getByTestId('member_icon_icon_symbol_robot')).toBeInTheDocument();
  });
});

describe('Invite form', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockGetOrganizationAction.isSuccess = true;
    mockGetOrganizationAction.lastSuccessResponse = mockOrganization;
    mockListMembersAction.isSuccess = true;
    mockListMembersAction.lastSuccessResponse = mockMembers;
  });

  it('when click invite button, shows invite form', async () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.title'));

    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.invite_form.toggle.show'));

    await waitFor(() =>
      expect(
        screen.getByPlaceholderText('pages.organizations.edit.tabs.members.invite_form.fields.email.placeholder')
      ).toBeInTheDocument()
    );
  });

  it('when click cancel, hides invite form', async () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.title'));
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.invite_form.toggle.show'));

    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.invite_form.toggle.cancel'));

    await waitFor(() =>
      expect(
        screen.queryByPlaceholderText('pages.organizations.edit.tabs.members.invite_form.fields.email.placeholder')
      ).not.toBeInTheDocument()
    );
  });

  it('when invalid email, shows validation error', async () => {
    renderWithTestingProviders(<OrganizationEditPage />, ['/organizations/anorganizationid1/edit']);
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.title'));
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.invite_form.toggle.show'));

    const emailInput = screen.getByPlaceholderText(
      'pages.organizations.edit.tabs.members.invite_form.fields.email.placeholder'
    );
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } });
    fireEvent.click(screen.getByText('pages.organizations.edit.tabs.members.invite_form.submit.label'));

    await waitFor(() =>
      expect(
        screen.getByText('pages.organizations.edit.tabs.members.invite_form.fields.email.validation')
      ).toBeInTheDocument()
    );
  });
});
