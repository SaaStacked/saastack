import { fireEvent, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Organization, OrganizationOnboardingStatus, OrganizationOwnership } from '../../../framework/api/apiHost1';
import { RoutePaths } from '../../../framework/constants.ts';
import { renderWithTestingProviders } from '../../../framework/testing/TestingProviders';
import { OnboardingReminder } from './OnboardingReminder';

const mockNavigate = vi.fn();
const mockLocation = { pathname: '/apath' };

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => mockLocation
  };
});

const mockCurrentUser = {
  userId: 'auserid1',
  profile: {
    userId: 'auserid1',
    emailAddress: 'auser@company.com',
    name: { firstName: 'afirstname', lastName: 'alastname' }
  },
  organization: {
    id: 'anorganizationid',
    name: 'anorganizationname',
    ownership: OrganizationOwnership.PERSONAL,
    onboardingStatus: OrganizationOnboardingStatus.IN_PROGRESS,
    createdById: 'auserid1'
  } as Organization,
  isSuccess: true,
  isExecuting: false,
  isAuthenticated: true,
  refetch: vi.fn()
};

vi.mock('../../../framework/providers/CurrentUserContext', async () => {
  const actual = await vi.importActual('../../../framework/providers/CurrentUserContext');
  return {
    ...actual,
    useCurrentUser: () => mockCurrentUser,
    CurrentUserProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>
  };
});

describe('OnboardingReminder', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockCurrentUser.organization = {
      id: 'anorganizationid',
      name: 'anorganizationname',
      ownership: OrganizationOwnership.PERSONAL,
      onboardingStatus: OrganizationOnboardingStatus.IN_PROGRESS,
      createdById: 'auserid1'
    };
    mockLocation.pathname = '/apath';
  });

  it('does not render when onboarding is complete', () => {
    mockCurrentUser.organization!.onboardingStatus = OrganizationOnboardingStatus.COMPLETE;

    renderWithTestingProviders(<OnboardingReminder />);

    expect(screen.queryByTestId('onboarding_reminder')).not.toBeInTheDocument();
  });

  it('does not render when organization is shared', () => {
    mockCurrentUser.organization!.ownership = OrganizationOwnership.SHARED;

    renderWithTestingProviders(<OnboardingReminder />);

    expect(screen.queryByTestId('onboarding_reminder')).not.toBeInTheDocument();
  });

  it('does not render when on onboarding page', () => {
    mockLocation.pathname = RoutePaths.OrganizationOnboarding;
    renderWithTestingProviders(<OnboardingReminder />, [RoutePaths.OrganizationOnboarding]);

    expect(screen.queryByTestId('onboarding_reminder')).not.toBeInTheDocument();
  });

  it('renders when onboarding is in progress', () => {
    renderWithTestingProviders(<OnboardingReminder />);

    expect(screen.getByTestId('onboarding_reminder')).toBeInTheDocument();
  });

  it('navigates to onboarding page when clicked', () => {
    renderWithTestingProviders(<OnboardingReminder />);

    const reminder = screen.getByTestId('onboarding_reminder');
    fireEvent.click(reminder);

    expect(mockNavigate).toHaveBeenCalledWith(RoutePaths.OrganizationOnboarding);
  });

  it('navigates to onboarding page when Enter key is pressed', () => {
    renderWithTestingProviders(<OnboardingReminder />);

    const reminder = screen.getByTestId('onboarding_reminder');
    fireEvent.keyDown(reminder, { key: 'Enter' });

    expect(mockNavigate).toHaveBeenCalledWith(RoutePaths.OrganizationOnboarding);
  });

  it('navigates to onboarding page when Space key is pressed', () => {
    renderWithTestingProviders(<OnboardingReminder />);

    const reminder = screen.getByTestId('onboarding_reminder');
    fireEvent.keyDown(reminder, { key: ' ' });

    expect(mockNavigate).toHaveBeenCalledWith(RoutePaths.OrganizationOnboarding);
  });

  it('does not navigate when other keys are pressed', () => {
    renderWithTestingProviders(<OnboardingReminder />);

    const reminder = screen.getByTestId('onboarding_reminder');
    fireEvent.keyDown(reminder, { key: 'Escape' });

    expect(mockNavigate).not.toHaveBeenCalled();
  });
});
