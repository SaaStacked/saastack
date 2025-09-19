import { logout } from '../../api/websiteHost';
import { useActionCommand } from '../ActionCommand.ts';


export const LogoutAction = () =>
  useActionCommand({
    request: () => logout(),
    onSuccess: () => window.location.reload() //so that we pick up the changed auth cookies, and return to dashboard page
  });
