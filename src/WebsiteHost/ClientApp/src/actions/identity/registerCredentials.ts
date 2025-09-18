import {
  registerPersonCredential,
  RegisterPersonCredentialRequest,
  RegisterPersonCredentialResponse
} from '../../api/apiHost1';
import { useActionCommand } from '../ActionCommand.ts';

export const RegisterCredentialsAction = () =>
  useActionCommand<RegisterPersonCredentialRequest, RegisterPersonCredentialResponse>({
    request: (request) =>
      registerPersonCredential({
        body: request
      })
  });
