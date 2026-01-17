import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { authenticate, AuthenticateRequest, AuthenticateResponse } from '../../../framework/api/websiteHost';
import { LocalStorageKeys, RoutePaths } from '../../../framework/constants.ts';

export enum LoginCredentialsErrors {
  account_locked = 'account_locked',
  account_unverified = 'account_unverified',
  invalid_credentials = 'invalid_credentials',
  mfa_required = 'mfa_required'
}

export const LoginCredentialsAction = () =>
  useActionCommand<AuthenticateRequest, AuthenticateResponse, LoginCredentialsErrors>({
    request: (request) =>
      authenticate({
        body: {
          ...request,
          provider: 'credentials'
        }
      }),
    passThroughErrors: {
      400: LoginCredentialsErrors.invalid_credentials,
      401: LoginCredentialsErrors.invalid_credentials,
      403: LoginCredentialsErrors.mfa_required,
      405: LoginCredentialsErrors.account_unverified,
      423: LoginCredentialsErrors.account_locked
    },
    onSuccess: () => {
      const isPendingAuthorize = localStorage.getItem(LocalStorageKeys.IsPendingOAuth2Authorization);
      if (isPendingAuthorize) {
        localStorage.removeItem(LocalStorageKeys.IsPendingOAuth2Authorization);
        window.location.href = RoutePaths.OAuth2Authorize; // no browser history
        return;
      }

      window.location.replace(RoutePaths.Home); //so that we reload index.html and pick up the changed auth cookies, and return to home page
    }
  });
