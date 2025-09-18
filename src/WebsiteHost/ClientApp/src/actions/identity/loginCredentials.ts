import { authenticate, AuthenticateRequest, AuthenticateResponse } from '../../api/websiteHost';
import { useActionCommand } from '../ActionCommand.ts';


export enum LoginErrors {
  account_locked = 'account_locked',
  account_unverified = 'account_unverified',
  invalid_credentials = 'invalid_credentials'
}

export const LoginCredentialsAction = () =>
  useActionCommand<AuthenticateRequest, AuthenticateResponse>({
    request: (request) =>
      authenticate({
        body: {
          ...request,
          provider: 'credentials'
        }
      }),
    passThroughErrors: {
      400: LoginErrors.account_locked,
      401: LoginErrors.invalid_credentials,
      405: LoginErrors.account_unverified,
      409: LoginErrors.account_locked
    },
    onSuccess: () => window.location.reload() //so that we pick up the changed auth cookies, and return to dashboard page
  });
