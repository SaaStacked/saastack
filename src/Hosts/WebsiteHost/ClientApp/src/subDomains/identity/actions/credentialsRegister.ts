import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  registerPersonCredential,
  RegisterPersonCredentialRequest,
  RegisterPersonCredentialResponse
} from '../../../framework/api/apiHost1';

export const CredentialsRegisterAction = () =>
  useActionCommand<RegisterPersonCredentialRequest, RegisterPersonCredentialResponse>({
    request: (request) =>
      registerPersonCredential({
        body: request
      })
  });
