import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  confirmPersonCredentialRegistration,
  ConfirmPersonCredentialRegistrationRequest,
  EmptyResponse
} from '../../../framework/api/apiHost1';

export enum ConfirmRegisterErrors {
  token_expired = 'token_expired',
  token_used = 'token_used'
}

export const CredentialsRegisterConfirmAction = () =>
  useActionCommand<ConfirmPersonCredentialRegistrationRequest, EmptyResponse>({
    request: (request) =>
      confirmPersonCredentialRegistration({
        body: request
      }),
    passThroughErrors: {
      400: ConfirmRegisterErrors.token_expired,
      404: ConfirmRegisterErrors.token_used
    }
  });
