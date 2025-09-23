import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import Button from '../../../framework/components/button/Button.tsx';
import Card from '../../../framework/components/form/Card.tsx';


export function HomeAnonymousPage() {
  const { t: translate } = useTranslation();
  return (
    <Card>
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
          <Link to="/identity/credentials/register">{translate('pages.home.home_anonymous.links.register.text')}</Link>
        </div>
      </div>
    </Card>
  );
}
