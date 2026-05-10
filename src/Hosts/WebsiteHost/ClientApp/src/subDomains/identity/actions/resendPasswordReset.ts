import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  resendPasswordReset,
  ResendPasswordResetRequest,
  ResendPasswordResetResponse
} from '../../../framework/api/apiHost1';

export enum ResendPasswordResetErrors {
  user_not_registered = 'user_not_registered',
  token_invalid = 'token_invalid'
}

export const ResendPasswordResetAction = () =>
  useActionCommand<ResendPasswordResetRequest, ResendPasswordResetResponse, ResendPasswordResetErrors>({
    request: (request) =>
      resendPasswordReset({
        body: request
      }),
    passThroughErrors: {
      404: ResendPasswordResetErrors.token_invalid,
      405: ResendPasswordResetErrors.user_not_registered
    }
  });
