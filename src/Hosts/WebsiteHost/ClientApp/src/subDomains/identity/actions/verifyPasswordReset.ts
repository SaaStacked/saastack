import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  verifyPasswordReset,
  VerifyPasswordResetData,
  VerifyPasswordResetResponse
} from '../../../framework/api/apiHost1';


export enum VerifyPasswordResetErrors {
  token_expired = 'token_expired',
  token_invalid = 'token_invalid'
}

export const VerifyPasswordResetAction = () =>
  useActionCommand<VerifyPasswordResetData, VerifyPasswordResetResponse, VerifyPasswordResetErrors>({
    request: (request) => verifyPasswordReset(request),
    passThroughErrors: {
      404: VerifyPasswordResetErrors.token_invalid,
      405: VerifyPasswordResetErrors.token_expired
    }
  });
