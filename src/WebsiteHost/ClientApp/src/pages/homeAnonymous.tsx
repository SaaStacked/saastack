import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import Button from '../components/button/Button.tsx';


export function HomeAnonymousPage() {
  const { t: translate } = useTranslation();
  return (
    <div className="container min-h-screen flex items-center justify-center">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <div className="text-4xl font-bold text-center mb-16">
          <h1>{translate('pages.home.home_anonymous.title')}</h1>
        </div>

        <div className="container flex flex-col items-center space-y-4">
          <Button className="w-4/5 rounded-full" variant="outline" navigateTo="/identity/sso/microsoft">
            <img src="/images/microsoft-logo.svg" width={48} height={48} alt="Microsoft" />
            <span>&nbsp;&nbsp;{translate('pages.home.home_anonymous.providers.microsoft')}</span>
          </Button>

          <Button className="w-4/5 rounded-full" variant="outline" navigateTo="/identity/credentials/login">
            <img src="/images/email-icon.svg" width={48} height={48} alt="Email" />
            <span>&nbsp;&nbsp;{translate('pages.home.home_anonymous.providers.credentials')}</span>
          </Button>

          <div className="justify-center">
            <span className="px-3">{translate('pages.home.home_anonymous.links.register.question')}&nbsp;</span>
            <Link to="/identity/credentials/register">
              {translate('pages.home.home_anonymous.links.register.text')}
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
