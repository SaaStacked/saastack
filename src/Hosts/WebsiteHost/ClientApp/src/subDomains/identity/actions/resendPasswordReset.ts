import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  resendPasswordReset,
  ResendPasswordResetData,
  ResendPasswordResetResponse
} from '../../../framework/api/apiHost1';

export enum ResendPasswordResetErrors {
  user_not_registered = 'user_not_registered',
  token_invalid = 'token_invalid'
}

export const ResendPasswordResetAction = () =>
  useActionCommand<ResendPasswordResetData, ResendPasswordResetResponse, ResendPasswordResetErrors>({
    request: (request) => resendPasswordReset(request),
    passThroughErrors: {
      404: ResendPasswordResetErrors.token_invalid,
      405: ResendPasswordResetErrors.user_not_registered
    }
  });
