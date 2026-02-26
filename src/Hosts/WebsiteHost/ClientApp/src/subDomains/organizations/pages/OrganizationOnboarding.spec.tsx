import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ActionResult } from '../../../framework/actions/Actions';
import {
  GetOnboardingResponse,
  Organization,
  OrganizationOnboardingStatus,
  OrganizationOnboardingStepSchemaType,
  OrganizationOnboardingWorkflow,
  OrganizationOwnership
} from '../../../framework/api/apiHost1';
import { RoutePaths } from '../../../framework/constants.ts';
import { renderWithTestingProviders } from '../../../framework/testing/TestingProviders';
import { OrganizationOnboardingPage } from './OrganizationOnboarding';

const mockNavigate = vi.fn();

const mockWorkflow: OrganizationOnboardingWorkflow = {
  id: 'aworkflowid',
  organizationId: 'anorganizationid',
  workflow: {
    name: 'aname',
    startStepId: 'start',
    endStepId: 'end',
    steps: {
      start: {
        id: 'start',
        title: 'start',
        description: 'adescription',
        type: OrganizationOnboardingStepSchemaType.START,
        weight: 40,
        nextStepId: 'setup',
        branches: [],
        initialValues: {}
      },
      setup: {
        id: 'setup',
        title: 'setup',
        description: 'adescription',
        type: OrganizationOnboardingStepSchemaType.NORMAL,
        weight: 60,
        nextStepId: 'end',
        branches: [],
        initialValues: {}
      },
      end: {
        id: 'end',
        title: 'end',
        description: 'adescription',
        type: OrganizationOnboardingStepSchemaType.END,
        weight: 0,
        branches: [],
        initialValues: {}
      }
    }
  },
  state: {
    status: OrganizationOnboardingStatus.IN_PROGRESS,
    currentStep: {
      id: 'start',
      title: 'start',
      weight: 40,
      values: {}
    },
    pathTaken: [],
    pathAhead: [
      {
        id: 'setup',
        title: 'setup',
        weight: 60,
        values: {}
      },
      {
        id: 'end',
        title: 'end',
        weight: 0,
        values: {}
      }
    ],
    values: {},
    progressPercentage: 0,
    completedWeight: 0,
    totalWeight: 100,
    startedAt: new Date('2024-01-01T00:00:00Z')
  }
};

const mockOrganization = {
  id: 'anorganizationid',
  name: 'anorganizationname',
  ownership: OrganizationOwnership.PERSONAL,
  onboardingStatus: OrganizationOnboardingStatus.IN_PROGRESS,
  createdById: 'auserid'
} as Organization;

const mockGetOrganization = vi.fn(() => mockOrganization);

const mockCurrentUser = {
  userId: 'auserid',
  profile: {
    userId: 'auserid',
    emailAddress: 'auser@company.com',
    name: { firstName: 'afirstname', lastName: 'alastname' }
  },
  get organization() {
    return mockGetOrganization();
  },
  isSuccess: true,
  isExecuting: false,
  isAuthenticated: true,
  refetch: vi.fn()
};

const mockGetOnboardingAction: ActionResult<any, any, OrganizationOnboardingWorkflow> = {
  execute: vi.fn(),
  isSuccess: true,
  lastSuccessResponse: mockWorkflow,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockInitiateAction: ActionResult<any, any, GetOnboardingResponse> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockMoveForwardAction: ActionResult<any, any, GetOnboardingResponse> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockMoveBackAction: ActionResult<any, any, GetOnboardingResponse> = {
  execute: vi.fn(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined
};

const mockCompleteAction: ActionResult<any, any, GetOnboardingResponse> = {
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
    useNavigate: () => mockNavigate
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

vi.mock('../actions/getOnboardingWorkflow', () => ({
  GetOnboardingWorkflowAction: () => mockGetOnboardingAction,
  GetOnboardingErrorCodes: {
    not_initiated: 'not_initiated'
  }
}));

vi.mock('../actions/initiateOnboardingWorkflow', () => ({
  InitiateOnboardingWorkflowAction: () => mockInitiateAction,
  OnboardingInitiationErrorCodes: {
    already_initiated: 'already_initiated'
  }
}));

vi.mock('../actions/moveForwardWorkflowStep', () => ({
  MoveForwardWorkflowStepAction: () => mockMoveForwardAction
}));

vi.mock('../actions/moveBackWorkflowStep', () => ({
  MoveBackWorkflowStepAction: () => mockMoveBackAction,
  OnboardingNavigationErrorCodes: {
    invalid_direction: 'invalid_direction'
  }
}));

vi.mock('../actions/completeOnboardingWorkflow', () => ({
  CompleteOnboardingWorkflowAction: () => mockCompleteAction
}));

describe('OrganizationOnboardingPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetOrganization.mockReturnValue(mockOrganization);
    mockGetOnboardingAction.isSuccess = true;
    mockGetOnboardingAction.lastSuccessResponse = mockWorkflow;
    mockGetOnboardingAction.execute = vi.fn();
    mockInitiateAction.execute = vi.fn();
  });

  it('when onboarding in progress, displays first step', async () => {
    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() => {
      expect(screen.getByText('pages.organizations.onboarding.title')).toBeInTheDocument();
      expect(screen.getByTestId('onboarding_step_start')).toBeInTheDocument();
    });
  });

  it('when onboarding not started, initiates workflow, and then fetches onboarding', async () => {
    // sequence the calls
    mockGetOrganization
      .mockReturnValueOnce({
        id: 'anorganizationid',
        name: 'anorganizationname',
        ownership: OrganizationOwnership.PERSONAL,
        onboardingStatus: OrganizationOnboardingStatus.NOT_STARTED,
        createdById: 'auserid'
      })
      .mockReturnValue({
        id: 'anorganizationid',
        name: 'anorganizationname',
        ownership: OrganizationOwnership.PERSONAL,
        onboardingStatus: OrganizationOnboardingStatus.IN_PROGRESS,
        createdById: 'auserid'
      });

    mockInitiateAction.execute = vi.fn((_, callbacks) =>
      callbacks?.onSuccess?.({ response: { workflow: mockWorkflow } })
    );

    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() => expect(mockInitiateAction.execute).toHaveBeenCalled());

    //re-render the control the second time
    render(<OrganizationOnboardingPage />);

    await waitFor(() => expect(mockGetOnboardingAction.execute).toHaveBeenCalled());
  });

  it('when onboarding complete, navigates to home', () => {
    mockGetOrganization.mockReturnValue({
      id: 'anorganizationid',
      name: 'anorganizationname',
      ownership: OrganizationOwnership.PERSONAL,
      onboardingStatus: OrganizationOnboardingStatus.COMPLETE,
      createdById: 'auserid'
    });

    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    expect(mockNavigate).toHaveBeenCalledWith('/');
  });

  it('when shared organization, navigates to home', () => {
    mockGetOrganization.mockReturnValue({
      id: 'anorganizationid',
      name: 'anorganizationname',
      ownership: OrganizationOwnership.SHARED,
      onboardingStatus: OrganizationOnboardingStatus.IN_PROGRESS,
      createdById: 'auserid'
    });

    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    expect(mockNavigate).toHaveBeenCalledWith('/');
  });
});

describe('OnboardingWorkflow - Navigation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetOrganization.mockReturnValue(mockOrganization);
    mockGetOnboardingAction.isSuccess = true;
    mockGetOnboardingAction.lastSuccessResponse = mockWorkflow;
  });

  it('when on first step, does not show back button', async () => {
    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() => expect(screen.getByTestId('onboarding_step_start')).toBeInTheDocument());

    expect(screen.queryByTestId('onboarding_back_button_action_button')).not.toBeInTheDocument();
  });

  it('when on middle step, shows both back and next buttons', async () => {
    mockGetOnboardingAction.lastSuccessResponse = {
      ...mockWorkflow,
      state: {
        ...mockWorkflow.state!,
        currentStep: {
          id: 'setup',
          title: 'setup',
          weight: 60,
          values: {}
        },
        progressPercentage: 40
      }
    };

    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() => {
      expect(screen.getByTestId('onboarding_back_button_action_button')).toBeInTheDocument();
      expect(screen.getByTestId('onboarding_next_button_action_button')).toBeInTheDocument();
    });
  });

  it('when on last step, shows complete button instead of next', async () => {
    mockGetOnboardingAction.lastSuccessResponse = {
      ...mockWorkflow,
      state: {
        ...mockWorkflow.state!,
        currentStep: {
          id: 'end',
          title: 'end',
          weight: 0,
          values: {}
        },
        progressPercentage: 100
      }
    };

    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() => {
      expect(screen.queryByTestId('onboarding_complete_button_action_button')).toBeInTheDocument();
      expect(screen.queryByTestId('onboarding_next_button_action_button')).not.toBeInTheDocument();
    });
  });

  it('when click next button, moves forward', async () => {
    const updatedWorkflow = {
      ...mockWorkflow,
      state: {
        ...mockWorkflow.state!,
        currentStep: {
          id: 'setup',
          title: 'setup',
          weight: 60,
          values: {}
        }
      }
    };

    mockMoveForwardAction.execute = vi.fn((_, callbacks) =>
      callbacks?.onSuccess?.({ response: { workflow: updatedWorkflow } })
    );

    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() => expect(screen.queryByTestId('onboarding_next_button_action_button')).toBeInTheDocument());

    fireEvent.click(screen.getByTestId('onboarding_next_button_action_button'));

    await waitFor(() => expect(mockMoveForwardAction.execute).toHaveBeenCalled());
  });

  it('when click back button, moves backward', async () => {
    mockGetOnboardingAction.lastSuccessResponse = {
      ...mockWorkflow,
      state: {
        ...mockWorkflow.state!,
        currentStep: {
          id: 'setup',
          title: 'setup',
          weight: 60,
          values: {}
        }
      }
    };

    const updatedWorkflow = {
      ...mockWorkflow,
      state: {
        ...mockWorkflow.state!,
        currentStep: {
          id: 'start',
          title: 'start',
          weight: 40,
          values: {}
        }
      }
    };

    mockMoveBackAction.execute = vi.fn((_, callbacks) =>
      callbacks?.onSuccess?.({ response: { workflow: updatedWorkflow } })
    );

    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() => expect(screen.queryByTestId('onboarding_back_button_action_button')).toBeInTheDocument());

    fireEvent.click(screen.getByTestId('onboarding_back_button_action_button'));

    await waitFor(() => expect(mockMoveBackAction.execute).toHaveBeenCalled());
  });

  it('when click complete button, completes onboarding and navigates home', async () => {
    const workflowOnEnd = {
      ...mockWorkflow,
      state: {
        ...mockWorkflow.state!,
        currentStep: {
          id: 'end',
          title: 'end',
          weight: 0,
          values: {}
        },
        status: OrganizationOnboardingStatus.COMPLETE
      }
    };
    mockGetOnboardingAction.lastSuccessResponse = workflowOnEnd;

    mockCompleteAction.execute = vi.fn((_, callbacks) =>
      callbacks?.onSuccess?.({ response: { workflow: workflowOnEnd } })
    );

    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() => expect(screen.getByTestId('onboarding_complete_button_action_button')).toBeInTheDocument());

    fireEvent.click(screen.getByTestId('onboarding_complete_button_action_button'));

    await waitFor(() => {
      expect(mockCompleteAction.execute).toHaveBeenCalled();
      expect(mockCurrentUser.refetch).toHaveBeenCalled();
      expect(mockNavigate).toHaveBeenCalledWith('/');
    });
  });
});

describe('OnboardingWorkflow - ProgressBar', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetOrganization.mockReturnValue(mockOrganization);
    mockGetOnboardingAction.isSuccess = true;
    mockGetOnboardingAction.lastSuccessResponse = mockWorkflow;
  });

  it('displays progress percentage', async () => {
    mockGetOnboardingAction.lastSuccessResponse = {
      ...mockWorkflow,
      state: {
        ...mockWorkflow.state!,
        progressPercentage: 40
      }
    };

    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() =>
      expect(screen.getByText('40% pages.organizations.onboarding.labels.progress_complete')).toBeInTheDocument()
    );
  });

  it('displays all workflow steps', async () => {
    renderWithTestingProviders(<OrganizationOnboardingPage />, [RoutePaths.OrganizationOnboarding]);

    await waitFor(() => {
      expect(screen.getByTestId('onboarding_step_start_label')).toBeInTheDocument();
      expect(screen.getByTestId('onboarding_step_setup_label')).toBeInTheDocument();
      expect(screen.getByTestId('onboarding_step_end_label')).toBeInTheDocument();
    });
  });
});
