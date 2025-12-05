import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  completePasswordReset,
  CompletePasswordResetData,
  CompletePasswordResetResponse
} from '../../../framework/api/apiHost1';

export enum CompletePasswordResetErrors {
  token_expired = 'token_expired',
  token_invalid = 'token_invalid',
  invalid_password = 'invalid_password',
  duplicate_password = 'duplicate_password'
}

export const CompletePasswordResetAction = () =>
  useActionCommand<CompletePasswordResetData, CompletePasswordResetResponse, CompletePasswordResetErrors>({
    request: (request) => completePasswordReset(request),
    passThroughErrors: {
      400: CompletePasswordResetErrors.invalid_password,
      404: CompletePasswordResetErrors.token_invalid,
      405: CompletePasswordResetErrors.token_expired,
      409: CompletePasswordResetErrors.duplicate_password
    }
  });
