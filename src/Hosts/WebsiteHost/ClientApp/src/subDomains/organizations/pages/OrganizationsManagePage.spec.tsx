import { fireEvent, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ActionResult } from '../../../framework/actions/Actions';
import { OrganizationOwnership } from '../../../framework/api/apiHost1';
import { renderWithTestingProviders } from '../../../framework/testing/TestingProviders';
import { TenantRoles } from './Organizations';
import { OrganizationsManagePage } from './OrganizationsManagePage';

const mockOrganization1 = {
  id: 'anorganizationid1',
  name: 'anorganizationname1',
  ownership: OrganizationOwnership.SHARED,
  avatarUrl: null
};

const mockOrganization2 = {
  id: 'anorganizationid2',
  name: 'anorganizationname2',
  ownership: OrganizationOwnership.PERSONAL,
  avatarUrl: 'https://example.com/avatar.jpg'
};

const mockMemberships = [
  {
    organizationId: 'anorganizationid1',
    isDefault: true,
    roles: [TenantRoles.Owner],
    features: ['afeature1', 'afeature2']
  },
  {
    organizationId: 'anorganizationid2',
    isDefault: false,
    roles: [TenantRoles.Member],
    features: ['afeature3']
  }
];

const mockCurrentUser = {
  userId: 'auserid1',
  profile: {
    userId: 'auserid1',
    emailAddress: 'auser1@company.com',
    name: { firstName: 'afirstname', lastName: 'alastname' },
    memberships: mockMemberships
  },
  organization: mockOrganization1,
  isSuccess: true,
  isExecuting: false,
  isAuthenticated: true,
  refetch: vi.fn()
};

const mockListAllMembershipsAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: true,
  lastSuccessResponse: mockMemberships,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockGetOrganizationAction1: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: true,
  lastSuccessResponse: mockOrganization1,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockGetOrganizationAction2: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: true,
  lastSuccessResponse: mockOrganization2,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockChangeDefaultOrganizationAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

vi.mock('../../../framework/providers/CurrentUserContext', async () => {
  const actual = await vi.importActual('../../../framework/providers/CurrentUserContext');
  return {
    ...actual,
    useCurrentUser: () => mockCurrentUser,
    CurrentUserProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>
  };
});

vi.mock('../../endUsers/actions/listAllMemberships', () => ({
  ListAllMembershipsAction: () => mockListAllMembershipsAction
}));

vi.mock('../actions/getOrganization', () => ({
  GetOrganizationAction: (organizationId: string) => {
    if (organizationId === 'anorganizationid1') return mockGetOrganizationAction1;
    if (organizationId === 'anorganizationid2') return mockGetOrganizationAction2;
    return mockGetOrganizationAction1;
  },
  OrganizationErrorCodes: {
    forbidden: 'forbidden'
  }
}));

vi.mock('../../endUsers/actions/changeDefaultOrganization', () => ({
  ChangeDefaultOrganizationAction: () => mockChangeDefaultOrganizationAction
}));

describe('OrganizationsManagePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockListAllMembershipsAction.isSuccess = true;
    mockListAllMembershipsAction.lastSuccessResponse = mockMemberships;
    mockGetOrganizationAction1.isSuccess = true;
    mockGetOrganizationAction1.lastSuccessResponse = mockOrganization1;
    mockGetOrganizationAction2.isSuccess = true;
    mockGetOrganizationAction2.lastSuccessResponse = mockOrganization2;
  });

  it('renders page title', () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    expect(screen.getByText('pages.organizations.manage.title')).toBeInTheDocument();
  });

  it('displays list of organizations', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => {
      expect(screen.getByText('anorganizationname1')).toBeInTheDocument();
      expect(screen.getByText('anorganizationname2')).toBeInTheDocument();
    });
  });

  it('shows current tag for default organization', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByText('pages.organizations.manage.labels.current')).toBeInTheDocument());
  });

  it('displays organization roles', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByText('pages.organizations.labels.roles.tnt_own')).toBeInTheDocument());
  });

  it('displays organization features', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByText('pages.organizations.labels.features.afeature1')).toBeInTheDocument());
  });

  it('shows link to create new organization', () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    expect(screen.getByText('pages.organizations.manage.links.new')).toBeInTheDocument();
  });

  it('when no organizations, shows empty state', async () => {
    mockListAllMembershipsAction.lastSuccessResponse = [];

    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByText('pages.organizations.manage.empty')).toBeInTheDocument());
  });
});

describe('Organization Card', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockListAllMembershipsAction.isSuccess = true;
    mockListAllMembershipsAction.lastSuccessResponse = mockMemberships;
    mockGetOrganizationAction1.isSuccess = true;
    mockGetOrganizationAction1.lastSuccessResponse = mockOrganization1;
    mockGetOrganizationAction2.isSuccess = true;
    mockGetOrganizationAction2.lastSuccessResponse = mockOrganization2;
  });

  it('displays organization name', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByText('anorganizationname1')).toBeInTheDocument());
  });

  it('when personal organization, shows lock icon', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByText('anorganizationname2')).toBeInTheDocument());

    expect(screen.getByTestId('personal_icon_icon_symbol_lock')).toBeInTheDocument();
  });

  it('shows edit icon for all organizations', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => {
      const editIcons = screen.getAllByTestId('edit_icon_icon_symbol_edit');
      expect(editIcons.length).toBeGreaterThan(0);
    });
  });

  it('when shared organization with owner role, shows members icon', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByTestId('members_icon_icon_symbol_group')).toBeInTheDocument());
  });

  it('when not default organization, shows switch button', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByTestId('switch_icon_icon_symbol_shuffle')).toBeInTheDocument());
  });

  it('when default organization, does not show switch button', async () => {
    mockListAllMembershipsAction.lastSuccessResponse = [mockMemberships[0]];

    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByText('anorganizationname1')).toBeInTheDocument());

    expect(screen.queryByTestId('switch_icon_icon_symbol_shuffle')).not.toBeInTheDocument();
  });

  it('when click switch button, changes default organization', async () => {
    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByTestId('switch_icon_icon_symbol_shuffle')).toBeInTheDocument());

    const switchButton = screen.getByTestId('switch_icon_icon_symbol_shuffle').closest('button');
    fireEvent.click(switchButton!);

    await waitFor(() => expect(mockChangeDefaultOrganizationAction.execute).toHaveBeenCalled());
  });

  it('when not owner of shared organization, does not show members icon', async () => {
    mockListAllMembershipsAction.lastSuccessResponse = [mockMemberships[1]];

    renderWithTestingProviders(<OrganizationsManagePage />, ['/organizations']);

    await waitFor(() => expect(screen.getByText('anorganizationname2')).toBeInTheDocument());

    expect(screen.queryByTestId('members_icon_icon_symbol_group')).not.toBeInTheDocument();
  });
});
