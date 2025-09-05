import { useActionCommand } from '../ActionCommand.tsx';
import { logout } from '../../api/websiteHost';

export const LogoutAction = () =>
  useActionCommand({
    request: () => logout(),
    onSuccess: () => window.location.reload()
  });
