import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { authenticate, AuthenticateRequest, AuthenticateResponse } from '../../../framework/api/websiteHost';


export enum LoginErrors {
  account_locked = 'account_locked',
  account_unverified = 'account_unverified',
  invalid_credentials = 'invalid_credentials',
  mfa_required = 'mfa_required'
}

export const CredentialsLoginAction = () =>
  useActionCommand<AuthenticateRequest, AuthenticateResponse, LoginErrors>({
    request: (request) =>
      authenticate({
        body: {
          ...request,
          provider: 'credentials'
        }
      }),
    passThroughErrors: {
      400: LoginErrors.invalid_credentials,
      401: LoginErrors.invalid_credentials,
      403: LoginErrors.mfa_required,
      405: LoginErrors.account_unverified,
      423: LoginErrors.account_locked
    },
    onSuccess: () => window.location.reload() //so that we pick up the changed auth cookies, and return to dashboard page
  });
