import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import {
  initiatePasswordReset,
  InitiatePasswordResetRequest,
  InitiatePasswordResetResponse
} from '../../../framework/api/apiHost1';


export enum InitiatePasswordResetErrors {
  user_not_registered = 'user_not_registered'
}

export const InitiatePasswordResetAction = () =>
  useActionCommand<InitiatePasswordResetRequest, InitiatePasswordResetResponse, InitiatePasswordResetErrors>({
    request: (request) =>
      initiatePasswordReset({
        body: request
      }),
    passThroughErrors: {
      405: InitiatePasswordResetErrors.user_not_registered
    }
  });
