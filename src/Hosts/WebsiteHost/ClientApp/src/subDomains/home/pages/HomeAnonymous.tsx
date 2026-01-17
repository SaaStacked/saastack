import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import Button from '../../../framework/components/button/Button.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import { RoutePaths } from '../../../framework/constants.ts';

export function HomeAnonymousPage() {
  const { t: translate } = useTranslation();
  return (
    <FormPage title={translate('pages.home.home_anonymous.title')}>
      <div className="container flex flex-col items-center space-y-4">
        {window.isTestingOnly && (
          <Button className="w-4/5 rounded-full" variant="outline" navigateTo="/identity/sso/fakeprovider">
            <img
              src="/images/fakesso-logo.svg"
              width={48}
              height={48}
              alt="Fake SSO Provider"
              className="dark:invert dark:hue-rotate-180"
            />
            <span className="ml-4">{translate('pages.home.home_anonymous.providers.fakesso')}</span>
          </Button>
        )}
        <Button className="w-4/5 rounded-full" variant="outline" navigateTo={RoutePaths.SsoMicrosoft}>
          <img
            src="/images/microsoft-logo.svg"
            width={48}
            height={48}
            alt="Microsoft"
            className="dark:invert dark:hue-rotate-180"
          />
          <span className="ml-4">{translate('pages.home.home_anonymous.providers.microsoft')}</span>
        </Button>
        <Button className="w-4/5 rounded-full" variant="outline" navigateTo={RoutePaths.SsoGoogle}>
          <img
            src="/images/google-logo.svg"
            width={48}
            height={48}
            alt="Google"
            className="dark:invert dark:hue-rotate-180"
          />
          <span className="ml-4">{translate('pages.home.home_anonymous.providers.google')}</span>
        </Button>
        <Button className="w-4/5 rounded-full" variant="outline" navigateTo={RoutePaths.CredentialsLogin}>
          <img src="/images/email-icon.svg" width={48} height={48} alt="Email" className="dark:invert" />
          <span className="ml-2">{translate('pages.home.home_anonymous.providers.credentials')}</span>
        </Button>
        <div className="justify-center">
          <span className="px-3">{translate('pages.home.home_anonymous.links.register.question')}&nbsp;</span>
          <Link to={RoutePaths.Register}>{translate('pages.home.home_anonymous.links.register.text')}</Link>
        </div>
      </div>
    </FormPage>
  );
}
