import { logout } from '../../api/websiteHost';
import { useActionCommand } from '../ActionCommand.tsx';


export const LogoutAction = () =>
  useActionCommand({
    request: () => logout(),
    onSuccess: () => window.location.reload()
  });
