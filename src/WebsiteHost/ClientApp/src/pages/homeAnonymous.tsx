import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import Button from '../components/button/Button.tsx';


export function HomeAnonymousPage() {
  const { t: translate } = useTranslation('common');
  return (
    <div className="min-h-screen flex items-center justify-center">
      <div>
        <div className="text-4xl font-bold text-center">
          <h1>{translate('pages.home.home_anonymous.title')}</h1>
        </div>

        <div className="flex flex-col mt-8 items-center space-y-4">
          <Button className="w-2/3 rounded-full" variant="outline" navigateTo="/identity/sso-microsoft">
            <img src="/microsoft-logo.svg" width={48} height={48} alt="Microsoft" />
            <span>&nbsp;&nbsp;{translate('pages.home.home_anonymous.providers.microsoft')}</span>
          </Button>

          <Button className="w-2/3 rounded-full" variant="outline" navigateTo="/identity/login-credentials">
            <img src="/email-icon.svg" width={48} height={48} alt="Email" />
            <span>&nbsp;&nbsp;{translate('pages.home.home_anonymous.providers.credentials')}</span>
          </Button>

          <div className="justify-center">
            <span className="px-3 bg-white text-gray-500">
              {translate('pages.home.home_anonymous.links.register.question')}&nbsp;
            </span>
            <Link to="/identity/register-credentials">
              {translate('pages.home.home_anonymous.links.register.text')}
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
