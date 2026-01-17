import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { authorizeOAuth2, AuthorizeOAuth2Request, AuthorizeOAuth2Response } from '../../../framework/api/websiteHost';
import { LocalStorageKeys, RoutePaths } from '../../../framework/constants.ts';

export enum AuthorizeOAuth2Errors {
  BadRequest = 'bad_request'
}

export const AuthorizeOAuth2Action = () =>
  useActionCommand<AuthorizeOAuth2Request, AuthorizeOAuth2Response>({
    request: (request) =>
      authorizeOAuth2({
        body: {
          ...request
        }
      }),
    passThroughErrors: {
      400: AuthorizeOAuth2Errors.BadRequest
    },
    onSuccess: (_requestData, response) => {
      const result = response.redirect;
      if (result.isLogin) {
        localStorage.setItem(LocalStorageKeys.IsPendingOAuth2Authorization, 'true');
        window.location.href = RoutePaths.CredentialsLogin; // no browser history
        return;
      }

      localStorage.removeItem(LocalStorageKeys.IsPendingOAuth2Authorization);
      window.location.replace(result.redirectUri); // no browser history
    }
  });
