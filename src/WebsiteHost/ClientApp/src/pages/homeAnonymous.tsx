import { Link } from 'react-router-dom';
import Button from '../components/button/Button.tsx';


export function HomeAnonymousPage() {
  return (
    <div className="min-h-screen flex items-center justify-center">
      <div>
        <div className="text-center">
          <h1>Welcome back to SaaStack</h1>
        </div>

        <div className="flex flex-col items-center">
          <Button className="w-2/3 rounded-full" navigateTo="/identity/login-sso-ms">
            <img src="/microsoft-logo.svg" width={48} height={48} alt="Microsoft" />
            <span>&nbsp;&nbsp;Sign in with Microsoft</span>
          </Button>

          <Button className="w-2/3 rounded-full" navigateTo="/identity/login-sso-google">
            <img src="/google-logo.svg" width={48} height={48} alt="Google" />
            <span>&nbsp;&nbsp;Sign in with Google</span>
          </Button>

          <Button className="w-2/3 rounded-full" navigateTo="/identity/login-credentials">
            <img src="/email-icon.svg" width={48} height={48} alt="Email" />
            <span>&nbsp;&nbsp;Sign in with Email</span>
          </Button>

          <div className="justify-center">
            <span className="px-3 bg-white text-gray-500">No account?&nbsp;</span>
            <Link to="/identity/register-credentials">Create one</Link>
          </div>
        </div>
      </div>
    </div>
  );
}
