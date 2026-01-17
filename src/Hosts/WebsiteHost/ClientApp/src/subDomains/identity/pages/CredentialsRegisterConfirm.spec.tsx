import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ActionResult, ErrorResponse } from '../../../framework/actions/Actions.ts';
import {
  ConfirmPersonCredentialRegistrationRequest,
  ConfirmPersonCredentialRegistrationResponse,
  ResendPersonCredentialRegistrationConfirmationRequest,
  ResendPersonCredentialRegistrationConfirmationResponse
} from '../../../framework/api/apiHost1';
import { ConfirmPersonCredentialRegistrationErrors } from '../actions/confirmPersonCredentialRegistration.ts';
import { CredentialsRegisterConfirm } from './CredentialsRegisterConfirm';


const mockConfirmAction: ActionResult<
  ConfirmPersonCredentialRegistrationRequest,
  ConfirmPersonCredentialRegistrationErrors,
  ConfirmPersonCredentialRegistrationResponse
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

const mockResendAction: ActionResult<
  ResendPersonCredentialRegistrationConfirmationRequest,
  ConfirmPersonCredentialRegistrationErrors,
  ResendPersonCredentialRegistrationConfirmationResponse
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

vi.mock('../actions/confirmPersonCredentialRegistration', () => ({
  ConfirmPersonCredentialRegistrationAction: () => mockConfirmAction,
  ConfirmPersonCredentialRegistrationErrors: {
    token_expired: 'token_expired',
    token_used: 'token_used'
  }
}));

vi.mock('../actions/resendPersonCredentialRegistrationConfirmation', () => ({
  ResendPersonCredentialRegistrationConfirmationAction: () => mockResendAction
}));

const renderWithRouter = (initialEntries: string[] = ['/']) =>
  render(
    <MemoryRouter initialEntries={initialEntries}>
      <CredentialsRegisterConfirm />
    </MemoryRouter>
  );

describe('CredentialsRegisterConfirm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockConfirmAction.execute = vi.fn();
    mockConfirmAction.isExecuting = false;
    mockConfirmAction.isSuccess = undefined;
    mockConfirmAction.lastSuccessResponse = undefined;
    mockConfirmAction.lastExpectedError = undefined;
    mockConfirmAction.lastUnexpectedError = undefined;

    mockResendAction.execute = vi.fn();
    mockResendAction.isExecuting = false;
    mockResendAction.isSuccess = undefined;
    mockResendAction.lastSuccessResponse = undefined;
    mockResendAction.lastExpectedError = undefined;
    mockResendAction.lastUnexpectedError = undefined;
  });

  describe('Confirming state', () =>
    it('when executing, displays loader', () => {
      mockConfirmAction.isExecuting = true;
      renderWithRouter(['/aroute?token=atoken']);

      expect(screen.getByTestId('credentials_register_confirm_page_action_loader_loader')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.states.confirming.loader')).toBeDefined();
      expect(mockConfirmAction.execute).toHaveBeenCalled();
    }));

  describe('Registered state', () => {
    beforeEach(() => {
      mockConfirmAction.isExecuting = false;
      mockConfirmAction.isSuccess = true;
    });

    it('when registered, displays instructions and navigation links', () => {
      renderWithRouter(['/aroute?token=atoken']);

      expect(screen.getByText('pages.identity.credentials_register_confirm.states.registered.title')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.states.registered.message')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.links.login')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.links.home')).toBeDefined();
    });
  });

  describe('Resent state', () => {
    beforeEach(() => {
      mockConfirmAction.isExecuting = false;
      mockConfirmAction.isSuccess = false;
      mockConfirmAction.lastExpectedError = {
        code: ConfirmPersonCredentialRegistrationErrors.token_expired
      };
      mockResendAction.lastExpectedError = undefined;
      mockResendAction.lastUnexpectedError = undefined;
    });

    it('when resend successful, displays success message', () => {
      mockResendAction.isSuccess = true;
      renderWithRouter(['/aroute?token=atoken']);

      expect(screen.getByTestId('resend_success_alert')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.states.resent.message')).toBeDefined();
    });
  });

  describe('Error states', () => {
    describe('invalid', () =>
      it('when no token in query string, displays error and navigation links', () => {
        renderWithRouter();

        expect(screen.getByText('pages.identity.credentials_register_confirm.states.invalid.title')).toBeDefined();
        expect(screen.getByTestId('error_token_missing_alert_message')).toBeDefined();
        expect(screen.getByText('pages.identity.credentials_register_confirm.links.home')).toBeDefined();
      }));
    describe('confirming', () => {
      beforeEach(() => {
        mockConfirmAction.isExecuting = false;
        mockConfirmAction.isSuccess = false;
        mockResendAction.lastExpectedError = undefined;
        mockResendAction.lastUnexpectedError = undefined;
      });

      it('when unexpected error, displays error', () => {
        mockConfirmAction.lastUnexpectedError = {
          data: {},
          response: {
            status: 500,
            statusText: 'Internal Server Error'
          } as Response
        } as ErrorResponse;
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('error_unexpected_unhandled_error')).toBeDefined();
      });

      it('when token used, displays error', () => {
        mockConfirmAction.lastExpectedError = {
          code: ConfirmPersonCredentialRegistrationErrors.token_used
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByText('pages.identity.credentials_register_confirm.states.confirming.title')).toBeDefined();
        expect(screen.getByTestId('error_token_used_alert')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_register_confirm.states.confirming.errors.token_used')
        ).toBeDefined();
        expect(screen.queryByTestId('resend_button_action_button')).toBeNull();
        expect(screen.getByText('pages.identity.credentials_register_confirm.links.home')).toBeDefined();
      });

      it('when token expired, displays error with resend button', () => {
        mockConfirmAction.lastExpectedError = {
          code: ConfirmPersonCredentialRegistrationErrors.token_expired
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByText('pages.identity.credentials_register_confirm.states.confirming.title')).toBeDefined();
        expect(screen.getByTestId('error_token_expired_alert')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_register_confirm.states.confirming.errors.token_expired')
        ).toBeDefined();
        expect(screen.getByTestId('resend_button_action_button')).toBeDefined();
        expect(screen.getByText('pages.identity.credentials_register_confirm.links.home')).toBeDefined();
      });
    });

    describe('resending', () => {
      beforeEach(() => {
        mockConfirmAction.isExecuting = false;
        mockConfirmAction.isSuccess = false;
        mockConfirmAction.lastExpectedError = {
          code: ConfirmPersonCredentialRegistrationErrors.token_expired
        };
        mockResendAction.lastExpectedError = undefined;
        mockResendAction.lastUnexpectedError = undefined;
      });

      it('when token used, displays error', () => {
        mockResendAction.lastExpectedError = {
          code: ConfirmPersonCredentialRegistrationErrors.token_used
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('resend_button_action_expected_error_alert_message')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_register_confirm.states.resending.errors.token_used')
        ).toBeDefined();
      });

      it('when unexpected error, displays error', () => {
        mockResendAction.lastUnexpectedError = {
          data: {},
          response: {
            status: 500,
            statusText: 'Internal Server Error'
          } as Response
        } as ErrorResponse;
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('resend_button_action_unexpected_error_unhandled_error')).toBeDefined();
      });
    });
  });
});
