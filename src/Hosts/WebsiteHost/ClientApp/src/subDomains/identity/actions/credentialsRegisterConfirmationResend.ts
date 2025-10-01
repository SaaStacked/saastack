import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  EmptyResponse,
  resendPersonCredentialRegistrationConfirmation,
  ResendPersonCredentialRegistrationConfirmationRequest
} from '../../../framework/api/apiHost1';
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
