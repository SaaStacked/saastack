import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  resendPersonCredentialRegistrationConfirmation,
  ResendPersonCredentialRegistrationConfirmationRequest,
  ResendPersonCredentialRegistrationConfirmationResponse
} from '../../../framework/api/apiHost1';

export enum ResendPersonCredentialRegistrationConfirmationErrors {
  already_registered = 'already_registered'
}

export const ResendPersonCredentialRegistrationConfirmationAction = () =>
  useActionCommand<
    ResendPersonCredentialRegistrationConfirmationRequest,
    ResendPersonCredentialRegistrationConfirmationResponse,
    ResendPersonCredentialRegistrationConfirmationErrors
  >({
    request: (request) =>
      resendPersonCredentialRegistrationConfirmation({
        body: request
      }),
    passThroughErrors: {
      404: ResendPersonCredentialRegistrationConfirmationErrors.already_registered
    }
  });
