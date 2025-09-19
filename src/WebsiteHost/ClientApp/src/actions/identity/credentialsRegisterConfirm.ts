import {
  confirmPersonCredentialRegistration,
  ConfirmPersonCredentialRegistrationRequest,
  EmptyResponse
} from '../../api/apiHost1';
import { useActionCommand } from '../ActionCommand.ts';

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
