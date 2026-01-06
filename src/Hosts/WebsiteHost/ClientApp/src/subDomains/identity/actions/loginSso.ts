import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { authenticate, AuthenticateRequest, AuthenticateResponse } from '../../../framework/api/websiteHost';
import { RoutePaths } from '../../../framework/constants.ts';
import { cleanupStoredPKCEParameters } from '../utils/OAuth2Security.ts';


export enum LoginSsoErrors {
  unauthorized = 'unauthorized'
}
export const LoginSsoAction = () =>
  useActionCommand<AuthenticateRequest, AuthenticateResponse, LoginSsoErrors>({
    request: (request) =>
      authenticate({
        body: {
          ...request
        }
      }),
    passThroughErrors: {
      400: LoginSsoErrors.unauthorized,
      401: LoginSsoErrors.unauthorized,
      405: LoginSsoErrors.unauthorized
    },
    onSuccess: () => {
      cleanupStoredPKCEParameters();

      window.location.replace(RoutePaths.Home); //so that we reload index.html and pick up the changed auth cookies, and return to home page
    }
  });
