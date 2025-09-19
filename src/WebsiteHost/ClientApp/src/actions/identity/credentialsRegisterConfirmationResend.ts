import {
  EmptyResponse,
  resendPersonCredentialRegistrationConfirmation,
  ResendPersonCredentialRegistrationConfirmationRequest
} from '../../api/apiHost1';
import { useActionCommand } from '../ActionCommand.ts';
import { ConfirmRegisterErrors } from './credentialsRegisterConfirm.ts';

export const CredentialsRegisterConfirmationResendAction = () =>
  useActionCommand<ResendPersonCredentialRegistrationConfirmationRequest, EmptyResponse>({
    request: (request) =>
      resendPersonCredentialRegistrationConfirmation({
        body: request
      }),
    passThroughErrors: {
      400: ConfirmRegisterErrors.token_expired,
      404: ConfirmRegisterErrors.token_used
    }
  });
