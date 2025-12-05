import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  confirmPersonCredentialRegistration,
  ConfirmPersonCredentialRegistrationRequest,
  ConfirmPersonCredentialRegistrationResponse
} from '../../../framework/api/apiHost1';

export enum ConfirmPersonCredentialRegistrationErrors {
  token_expired = 'token_expired',
  token_used = 'token_used'
}

export const ConfirmPersonCredentialRegistrationAction = () =>
  useActionCommand<
    ConfirmPersonCredentialRegistrationRequest,
    ConfirmPersonCredentialRegistrationResponse,
    ConfirmPersonCredentialRegistrationErrors
  >({
    request: (request) =>
      confirmPersonCredentialRegistration({
        body: request
      }),
    passThroughErrors: {
      400: ConfirmPersonCredentialRegistrationErrors.token_expired,
      404: ConfirmPersonCredentialRegistrationErrors.token_used
    }
  });
