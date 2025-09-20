import { useActionCommand } from '../../../framework/actions/ActionCommand.ts';
import { logout } from '../../../framework/api/websiteHost';

export const LogoutAction = () =>
  useActionCommand({
    request: () => logout(),
    onSuccess: () => window.location.reload() //so that we pick up the changed auth cookies, and return to dashboard page
  });
