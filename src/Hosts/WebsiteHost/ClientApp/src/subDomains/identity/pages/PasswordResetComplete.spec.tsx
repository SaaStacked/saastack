import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError } from 'axios';
import { ActionResult } from '../../../framework/actions/Actions.ts';
import {
  CompletePasswordResetRequest,
  CompletePasswordResetResponse,
  VerifyPasswordResetData,
  VerifyPasswordResetResponse
} from '../../../framework/api/apiHost1';
import { CompletePasswordResetErrors } from '../actions/completePasswordReset.ts';
import { VerifyPasswordResetErrors } from '../actions/verifyPasswordReset.ts';
import { PasswordResetCompletePage } from './PasswordResetComplete';

const mockVerifyAction: ActionResult<VerifyPasswordResetData, VerifyPasswordResetErrors, VerifyPasswordResetResponse> =
  {
    execute: vi.fn(),
    isExecuting: false,
    isSuccess: undefined,
    lastSuccessResponse: undefined,
    lastExpectedError: undefined,
    lastUnexpectedError: undefined,
    isReady: true,
    lastRequestValues: undefined
  };

const mockCompleteAction: ActionResult<
  CompletePasswordResetRequest,
  CompletePasswordResetErrors,
  CompletePasswordResetResponse
> = {
  execute: vi.fn(),
  isExecuting: false,
  isSuccess: undefined,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isReady: true,
  lastRequestValues: undefined
};

vi.mock('../actions/verifyPasswordReset', () => ({
  VerifyPasswordResetAction: () => mockVerifyAction,
  VerifyPasswordResetErrors: {
    token_expired: 'token_expired',
    token_invalid: 'token_invalid'
  }
}));

vi.mock('../actions/completePasswordReset', () => ({
  CompletePasswordResetAction: () => mockCompleteAction,
  CompletePasswordResetErrors: {
    token_expired: 'token_expired',
    token_invalid: 'token_invalid',
    invalid_password: 'invalid_password',
    duplicate_password: 'duplicate_password'
  }
}));

const renderWithRouter = (initialEntries: string[] = ['/']) =>
  render(
    <MemoryRouter initialEntries={initialEntries}>
      <PasswordResetCompletePage />
    </MemoryRouter>
  );

describe('PasswordResetCompletePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockVerifyAction.execute = vi.fn();
    mockVerifyAction.isExecuting = false;
    mockVerifyAction.isSuccess = undefined;
    mockVerifyAction.lastSuccessResponse = undefined;
    mockVerifyAction.lastExpectedError = undefined;
    mockVerifyAction.lastUnexpectedError = undefined;

    mockCompleteAction.execute = vi.fn();
    mockCompleteAction.isExecuting = false;
    mockCompleteAction.isSuccess = undefined;
    mockCompleteAction.lastSuccessResponse = undefined;
    mockCompleteAction.lastExpectedError = undefined;
    mockCompleteAction.lastUnexpectedError = undefined;
  });

  describe('Verifying state', () =>
    it('when executing, displays loader', () => {
      mockVerifyAction.isExecuting = true;
      renderWithRouter(['/aroute?token=atoken']);

      expect(screen.getByTestId('password_reset_verify_page_action_loader_loader')).toBeDefined();
      expect(
        screen.getByText('pages.identity.credentials_password_reset_complete.states.verifying.loader')
      ).toBeDefined();
      expect(mockVerifyAction.execute).toHaveBeenCalled();
    }));

  describe('Verified state', () => {
    beforeEach(() => {
      mockVerifyAction.isExecuting = false;
      mockVerifyAction.isSuccess = true;
    });

    it('when verified, displays password reset form', () => {
      renderWithRouter(['/aroute?token=atoken']);

      expect(screen.getByText('pages.identity.credentials_password_reset_complete.title')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_password_reset_complete.description')).toBeDefined();
      expect(screen.getByTestId('password_form_input_input')).toBeDefined();
      expect(screen.getByTestId('confirmPassword_form_input_input')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_password_reset_complete.links.login')).toBeDefined();
    });
  });

  describe('Completed state', () => {
    beforeEach(() => {
      mockVerifyAction.isExecuting = false;
      mockVerifyAction.isSuccess = true;
      mockCompleteAction.isExecuting = false;
      mockCompleteAction.isSuccess = true;
    });

    it('when completed, displays success message and navigation links', () => {
      renderWithRouter(['/aroute?token=atoken']);

      expect(
        screen.getByText('pages.identity.credentials_password_reset_complete.states.completed.title')
      ).toBeDefined();
      expect(
        screen.getByText('pages.identity.credentials_password_reset_complete.states.completed.message')
      ).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_password_reset_complete.links.login')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_password_reset_complete.links.home')).toBeDefined();
    });
  });

  describe('Error states', () => {
    describe('invalid', () =>
      it('when no token in query string, displays error and navigation links', () => {
        renderWithRouter();

        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.states.invalid.title')
        ).toBeDefined();
        expect(screen.getByTestId('error_token_missing_alert_message')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.links.request_reset')
        ).toBeDefined();
        expect(screen.getByText('pages.identity.credentials_password_reset_complete.links.home')).toBeDefined();
      }));

    describe('verifying', () => {
      beforeEach(() => {
        mockVerifyAction.isExecuting = false;
        mockVerifyAction.isSuccess = false;
      });

      it('when unexpected error, displays error', () => {
        mockVerifyAction.lastUnexpectedError = {
          response: { status: 500, statusText: 'Internal Server Error' },
          message: 'anerror'
        } as AxiosError;
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('password_reset_verify_page_action_unexpected_error_unhandled_error')).toBeDefined();
      });

      it('when token invalid, displays error', () => {
        mockVerifyAction.lastExpectedError = {
          code: VerifyPasswordResetErrors.token_invalid
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.states.verifying.title')
        ).toBeDefined();
        expect(screen.getByTestId('password_reset_verify_page_action_expected_error_alert')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.states.verifying.errors.token_invalid')
        ).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.links.request_reset')
        ).toBeDefined();
        expect(screen.getByText('pages.identity.credentials_password_reset_complete.links.home')).toBeDefined();
      });

      it('when token expired, displays error', () => {
        mockVerifyAction.lastExpectedError = {
          code: VerifyPasswordResetErrors.token_expired
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.states.verifying.title')
        ).toBeDefined();
        expect(screen.getByTestId('password_reset_verify_page_action_expected_error_alert')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.states.verifying.errors.token_expired')
        ).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.links.request_reset')
        ).toBeDefined();
        expect(screen.getByText('pages.identity.credentials_password_reset_complete.links.home')).toBeDefined();
      });
    });

    describe('completing', () => {
      beforeEach(() => {
        mockVerifyAction.isExecuting = false;
        mockVerifyAction.isSuccess = true;
        mockCompleteAction.isExecuting = false;
        mockCompleteAction.isSuccess = false;
      });

      it('when token invalid, displays error', () => {
        mockCompleteAction.lastExpectedError = {
          code: CompletePasswordResetErrors.token_invalid
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('password_reset_complete_form_action_expected_error_alert_message')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.states.completing.errors.token_invalid')
        ).toBeDefined();
      });

      it('when token expired, displays error', () => {
        mockCompleteAction.lastExpectedError = {
          code: CompletePasswordResetErrors.token_expired
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('password_reset_complete_form_action_expected_error_alert_message')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_password_reset_complete.states.completing.errors.token_expired')
        ).toBeDefined();
      });

      it('when invalid password, displays error', () => {
        mockCompleteAction.lastExpectedError = {
          code: CompletePasswordResetErrors.invalid_password
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('password_reset_complete_form_action_expected_error_alert_message')).toBeDefined();
        expect(
          screen.getByText(
            'pages.identity.credentials_password_reset_complete.states.completing.errors.invalid_password'
          )
        ).toBeDefined();
      });

      it('when duplicate password, displays error', () => {
        mockCompleteAction.lastExpectedError = {
          code: CompletePasswordResetErrors.duplicate_password
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('password_reset_complete_form_action_expected_error_alert_message')).toBeDefined();
        expect(
          screen.getByText(
            'pages.identity.credentials_password_reset_complete.states.completing.errors.duplicate_password'
          )
        ).toBeDefined();
      });

      it('when unexpected error, displays error', () => {
        mockCompleteAction.lastUnexpectedError = {
          response: { status: 500, statusText: 'Internal Server Error' },
          message: 'anerror'
        } as AxiosError;
        renderWithRouter(['/aroute?token=atoken']);

        expect(
          screen.getByTestId('password_reset_complete_form_action_unexpected_error_unhandled_error')
        ).toBeDefined();
      });
    });
  });
});
