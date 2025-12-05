import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  resendPersonCredentialRegistrationConfirmation,
  ResendPersonCredentialRegistrationConfirmationRequest,
  ResendPersonCredentialRegistrationConfirmationResponse
} from '../../../framework/api/apiHost1';
import { ConfirmPersonCredentialRegistrationErrors } from './confirmPersonCredentialRegistration.ts';

export const ResendPersonCredentialRegistrationConfirmationAction = (token: string) =>
  useActionCommand<
    ResendPersonCredentialRegistrationConfirmationRequest,
    ResendPersonCredentialRegistrationConfirmationResponse,
    ConfirmPersonCredentialRegistrationErrors
  >({
    request: (request) =>
      resendPersonCredentialRegistrationConfirmation({
        body: {
          ...request,
          token
        }
      }),
    passThroughErrors: {
      400: ConfirmPersonCredentialRegistrationErrors.token_expired,
      404: ConfirmPersonCredentialRegistrationErrors.token_used
    }
  });
