import { logout } from '../../api/websiteHost';
import { useActionCommand } from '../ActionCommand.ts';


export const LogoutAction = () =>
  useActionCommand({
    request: () => logout(),
    onSuccess: () => window.location.reload()
  });
