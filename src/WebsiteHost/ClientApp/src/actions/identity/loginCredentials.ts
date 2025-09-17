import { authenticate, AuthenticateData, AuthenticateResponse2 } from '../../api/websiteHost';
import { useActionCommand } from '../ActionCommand.ts';

export enum LoginError {
  ACCOUNT_LOCKED = 'ACCOUNT_LOCKED',
  ACCOUNT_UNVERIFIED = 'ACCOUNT_UNVERIFIED',
  INVALID_CREDENTIALS = 'INVALID_CREDENTIALS'
}

export const LoginCredentialsAction = () =>
  useActionCommand<AuthenticateData, AuthenticateResponse2>({
    request: (request) =>
      authenticate({
        body: {
          ...request.body,
          provider: 'credentials'
        }
      }),
    passThroughErrors: {
      400: LoginError.ACCOUNT_LOCKED,
      401: LoginError.INVALID_CREDENTIALS,
      405: LoginError.ACCOUNT_UNVERIFIED,
      409: LoginError.ACCOUNT_LOCKED
    },
    onSuccess: () => window.location.reload()
  });
