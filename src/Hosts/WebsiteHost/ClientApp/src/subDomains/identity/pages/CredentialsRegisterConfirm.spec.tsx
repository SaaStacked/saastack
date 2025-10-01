import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError } from 'axios';
import { ActionResult } from '../../../framework/actions/Actions.ts';
import { ConfirmPersonCredentialRegistrationRequest, EmptyResponse, ResendPersonCredentialRegistrationConfirmationRequest } from '../../../framework/api/apiHost1';
import { ConfirmRegisterErrors } from '../actions/credentialsRegisterConfirm.ts';
import { CredentialsRegisterConfirm } from './CredentialsRegisterConfirm';


const mockConfirmAction: ActionResult<
  ConfirmPersonCredentialRegistrationRequest,
  ConfirmRegisterErrors,
  EmptyResponse
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
  ConfirmRegisterErrors,
  EmptyResponse
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

vi.mock('../actions/credentialsRegisterConfirm', () => ({
  CredentialsRegisterConfirmAction: () => mockConfirmAction,
  ConfirmRegisterErrors: {
    token_expired: 'token_expired',
    token_used: 'token_used'
  }
}));

vi.mock('../actions/credentialsRegisterConfirmationResend', () => ({
  CredentialsRegisterConfirmationResendAction: () => mockResendAction
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

  describe('Confirming state', () => {
    it('when no token in query string, displays token missing error', () => {
      renderWithRouter();

      expect(screen.getByText('pages.identity.credentials_register_confirm.states.confirming.title')).toBeDefined();
      expect(screen.getByTestId('error_token_missing_alert_message')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.links.home')).toBeDefined();
    });

    it('when token present and executing, displays loader', () => {
      mockConfirmAction.isExecuting = true;
      renderWithRouter(['/aroute?token=atoken']);

      expect(screen.getByText('pages.identity.credentials_register_confirm.states.confirming.title')).toBeDefined();
      expect(screen.getByTestId('confirming_loader')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.states.confirming.message')).toBeDefined();
    });

    it('when token present and executed, displays title only', () => {
      renderWithRouter(['/aroute?token=atoken']);

      expect(screen.getByText('pages.identity.credentials_register_confirm.states.confirming.title')).toBeDefined();
      expect(screen.queryByTestId('confirming_loader')).toBeNull();
      expect(screen.queryByTestId('error_token_missing_alert_message')).toBeNull();
    });

    it('when token present, calls confirm registration', () => {
      renderWithRouter(['/aroute?token=atoken']);

      expect(mockConfirmAction.execute).toHaveBeenCalledWith({ token: 'atoken' });
    });
  });

  describe('Success state', () => {
    beforeEach(() => {
      mockConfirmAction.isSuccess = true;
    });

    it('displays success message and navigation links', () => {
      renderWithRouter(['/aroute?token=atoken']);

      expect(screen.getByText('pages.identity.credentials_register_confirm.states.success.title')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.states.success.message')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.links.login')).toBeDefined();
      expect(screen.getByText('pages.identity.credentials_register_confirm.links.home')).toBeDefined();
    });
  });

  describe('Error state', () => {
    describe('when token expired error', () => {
      beforeEach(() => {
        mockConfirmAction.lastExpectedError = {
          code: ConfirmRegisterErrors.token_expired
        };
        mockResendAction.lastExpectedError = undefined;
        mockResendAction.lastUnexpectedError = undefined;
      });

      it('displays error message with resend button', () => {
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByText('pages.identity.credentials_register_confirm.states.failed.title')).toBeDefined();
        expect(screen.getByTestId('error_token_expired_alert')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_register_confirm.states.failed.errors.token_expired')
        ).toBeDefined();
        expect(screen.getByTestId('resend_button')).toBeDefined();
        expect(screen.getByText('pages.identity.credentials_register_confirm.links.login')).toBeDefined();
        expect(screen.getByText('pages.identity.credentials_register_confirm.links.home')).toBeDefined();
      });

      it('when resend button clicked, calls resend action', async () => {
        renderWithRouter(['/aroute?token=atoken']);

        const resendButton = screen.getByTestId('resend_button');
        fireEvent.click(resendButton);

        expect(mockResendAction.execute).toHaveBeenCalledWith({ token: 'atoken' });
      });

      it('when resend successful, displays success message', () => {
        mockResendAction.isSuccess = true;
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('resend_success_alert')).toBeDefined();
        expect(screen.getByText('pages.identity.credentials_register_confirm.states.resend.success')).toBeDefined();
      });

      it('when resend return used token, displays error', () => {
        mockResendAction.lastExpectedError = {
          code: ConfirmRegisterErrors.token_used
        };
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('resend_error_token_used_alert')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_register_confirm.states.resend.errors.token_used')
        ).toBeDefined();
      });

      it('when resend returns unexpected error, displays unhandled error', () => {
        mockResendAction.lastUnexpectedError = {
          response: { status: 500, statusText: 'Internal Server Error' },
          message: 'anerror'
        } as AxiosError;
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('resend_error_unexpected_unhandled_error')).toBeDefined();
      });
    });

    describe('when token used error', () => {
      beforeEach(() => {
        mockConfirmAction.lastExpectedError = {
          code: ConfirmRegisterErrors.token_used
        };
      });

      it('displays error message', () => {
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByText('pages.identity.credentials_register_confirm.states.failed.title')).toBeDefined();
        expect(screen.getByTestId('error_token_used_alert')).toBeDefined();
        expect(
          screen.getByText('pages.identity.credentials_register_confirm.states.failed.errors.token_used')
        ).toBeDefined();
        expect(screen.queryByTestId('resend_button')).toBeNull();
        expect(screen.getByText('pages.identity.credentials_register_confirm.links.login')).toBeDefined();
        expect(screen.getByText('pages.identity.credentials_register_confirm.links.home')).toBeDefined();
      });
    });

    describe('when unexpected error', () => {
      beforeEach(() => {
        const mockError: AxiosError = {
          response: { status: 500, statusText: 'Internal Server Error' },
          message: 'anerror'
        } as AxiosError;
        mockConfirmAction.lastExpectedError = {
          code: ConfirmRegisterErrors.token_expired
        };
        mockConfirmAction.lastUnexpectedError = mockError;
      });

      it('displays unexpected error along with expected error', () => {
        renderWithRouter(['/aroute?token=atoken']);

        expect(screen.getByTestId('error_unexpected_unhandled_error')).toBeDefined();
      });
    });
  });
});
