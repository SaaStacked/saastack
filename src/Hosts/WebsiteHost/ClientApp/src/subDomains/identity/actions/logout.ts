import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { EmptyResponse } from '../../../framework/api/apiHost1';
import { EmptyRequest } from '../../../framework/api/apiHost1/emptyRequest.ts';
import { logout } from '../../../framework/api/websiteHost';


export const LogoutAction = () =>
  useActionCommand<EmptyRequest, EmptyResponse>({
    request: () => logout(),
    onSuccess: () => window.location.reload() //so that we pick up the changed auth cookies, and return to dashboard page
  });
