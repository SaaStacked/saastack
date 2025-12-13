import { fireEvent, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ActionResult } from '../../../framework/actions/Actions';
import { UserProfileClassification } from '../../../framework/api/apiHost1';
import { renderWithTestingProviders } from '../../../framework/testing/TestingProviders';
import { TenantFeatures, TenantRoles } from '../../organizations/pages/Organizations.ts';
import { PlatformFeatures, PlatformRoles } from './Profiles.ts';
import { ProfilesManagePage } from './ProfilesManage';

const mockProfile = {
  userId: 'auserid1',
  id: 'aprofileid1',
  emailAddress: 'auser@company.com',
  name: {
    firstName: 'afirstname',
    lastName: 'alastname'
  },
  displayName: 'adisplayname',
  locale: 'en',
  timezone: 'Pacific/Auckland',
  avatarUrl: null,
  roles: [PlatformRoles.Standard, TenantRoles.Owner],
  features: [PlatformFeatures.Basic, TenantFeatures.Basic],
  isAuthenticated: true,
  defaultOrganizationId: 'anorganizationid1',
  classification: UserProfileClassification.PERSON,
  address: {
    countryCode: 'NZ'
  },
  phoneNumber: null
};

const mockCurrentUser = {
  userId: 'auserid1',
  profile: mockProfile,
  organization: {
    id: 'anorganizationid1',
    name: 'anorganizationname'
  },
  isSuccess: true,
  isExecuting: false,
  isAuthenticated: true,
  refetch: vi.fn()
};

const mockGetProfileAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: true,
  lastSuccessResponse: mockProfile,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockChangeProfileAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockChangeAvatarAction: ActionResult<any, any, any> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockDeleteAvatarAction: ActionResult<any, any, any> = {
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

vi.mock('../actions/getProfileForCaller', () => ({
  GetProfileForCallerAction: () => mockGetProfileAction
}));

vi.mock('../actions/changeProfile', () => ({
  ChangeProfileAction: () => mockChangeProfileAction
}));

vi.mock('../actions/changeProfileAvatar', () => ({
  ChangeProfileAvatarAction: () => mockChangeAvatarAction,
  UploadAvatarErrors: {
    invalid_image: 'invalid_image'
  }
}));

vi.mock('../actions/deleteProfileAvatar', () => ({
  DeleteProfileAvatarAction: () => mockDeleteAvatarAction
}));

describe('ProfilesManagePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockGetProfileAction.isSuccess = true;
    mockGetProfileAction.lastSuccessResponse = mockProfile;
    mockChangeProfileAction.isSuccess = false;
    mockChangeAvatarAction.isSuccess = false;
    mockDeleteAvatarAction.isSuccess = false;
  });

  it('renders page title', () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    expect(screen.getByText('pages.profiles.manage.title')).toBeInTheDocument();
  });

  it('renders account and profile tabs', () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    expect(screen.getByText('pages.profiles.manage.tabs.account.title')).toBeInTheDocument();
    expect(screen.getByText('pages.profiles.manage.tabs.profile.title')).toBeInTheDocument();
  });
});

describe('Account Tab', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockGetProfileAction.isSuccess = true;
    mockGetProfileAction.lastSuccessResponse = mockProfile;
  });

  it('displays user name', async () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    await waitFor(() => expect(screen.getByText('adisplayname')).toBeInTheDocument());
  });

  it('displays email address', async () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    await waitFor(() => expect(screen.getByText('auser@company.com')).toBeInTheDocument());
  });

  it('displays user roles', async () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    await waitFor(() => expect(screen.getByText('pages.profiles.labels.roles.plt_std')).toBeInTheDocument());
  });

  it('displays user features', async () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    await waitFor(() => expect(screen.getByText('pages.profiles.labels.features.plt_basic')).toBeInTheDocument());
  });

  it('when no roles, shows empty state', async () => {
    mockGetProfileAction.lastSuccessResponse = {
      ...mockProfile,
      roles: []
    };

    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    await waitFor(() =>
      expect(screen.getByText('pages.profiles.manage.tabs.account.form.fields.roles.empty')).toBeInTheDocument()
    );
  });

  it('when no features, shows empty state', async () => {
    mockGetProfileAction.lastSuccessResponse = {
      ...mockProfile,
      features: []
    };

    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    await waitFor(() =>
      expect(screen.getByText('pages.profiles.manage.tabs.account.form.fields.features.empty')).toBeInTheDocument()
    );
  });
});

describe('Profile Tab', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockGetProfileAction.isSuccess = true;
    mockGetProfileAction.lastSuccessResponse = mockProfile;
  });

  it('displays avatar with first letter when no avatar url', async () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    const profileTab = screen.getByText('pages.profiles.manage.tabs.profile.title');
    fireEvent.click(profileTab);

    await waitFor(() => expect(screen.getByTestId('avatar_letter')).toBeInTheDocument());
  });

  it('displays avatar image when avatar url exists', async () => {
    mockGetProfileAction.lastSuccessResponse = {
      ...mockProfile,
      avatarUrl: 'https://example.com/avatar.jpg'
    };

    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    const profileTab = screen.getByText('pages.profiles.manage.tabs.profile.title');
    fireEvent.click(profileTab);

    await waitFor(() => {
      const img = screen.getByAltText('adisplayname');
      expect(img).toBeInTheDocument();
      expect(img).toHaveAttribute('src', 'https://example.com/avatar.jpg');
    });
  });

  it('shows upload avatar button', async () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    const profileTab = screen.getByText('pages.profiles.manage.tabs.profile.title');
    fireEvent.click(profileTab);

    await waitFor(() => expect(screen.getByTestId('upload_avatar_upload_button')).toBeInTheDocument());
  });

  it('when avatar exists, shows delete avatar button', async () => {
    mockGetProfileAction.lastSuccessResponse = {
      ...mockProfile,
      avatarUrl: 'https://example.com/avatar.jpg'
    };

    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    const profileTab = screen.getByText('pages.profiles.manage.tabs.profile.title');
    fireEvent.click(profileTab);

    await waitFor(() => expect(screen.getByTestId('delete_avatar_button_action_button')).toBeInTheDocument());
  });

  it('when no avatar, does not show delete avatar button', async () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    const profileTab = screen.getByText('pages.profiles.manage.tabs.profile.title');
    fireEvent.click(profileTab);

    await waitFor(() => expect(screen.queryByTestId('delete_avatar_button')).not.toBeInTheDocument());
  });

  it('displays profile form fields', async () => {
    renderWithTestingProviders(<ProfilesManagePage />, ['/profiles/me']);

    const profileTab = screen.getByText('pages.profiles.manage.tabs.profile.title');
    fireEvent.click(profileTab);

    await waitFor(() => {
      expect(screen.getByTestId('displayName_form_input_input')).toBeInTheDocument();
      expect(screen.getByTestId('locale_form_select_select')).toBeInTheDocument();
      expect(screen.getByTestId('timezone_form_select_select')).toBeInTheDocument();
    });
  });
});
