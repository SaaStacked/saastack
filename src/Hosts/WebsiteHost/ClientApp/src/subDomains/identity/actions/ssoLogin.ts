import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { authenticate, AuthenticateRequest, AuthenticateResponse } from '../../../framework/api/websiteHost';
import { cleanupStoredPKCEParameters } from '../utils/OAuth2Security.ts';
import { LoginErrors } from './credentialsLogin.ts';


export const SsoLoginAction = () =>
  useActionCommand<AuthenticateRequest, AuthenticateResponse, LoginErrors>({
    request: (request) =>
      authenticate({
        body: {
          ...request
        }
      }),
    passThroughErrors: {
      400: LoginErrors.invalid_credentials,
      401: LoginErrors.invalid_credentials,
      403: LoginErrors.mfa_required,
      405: LoginErrors.account_unverified,
      423: LoginErrors.account_locked
    },
    onSuccess: () => {
      cleanupStoredPKCEParameters();
      window.location.href = '/';
    } //so that we pick up the changed auth cookies, and return to dashboard page
  });
