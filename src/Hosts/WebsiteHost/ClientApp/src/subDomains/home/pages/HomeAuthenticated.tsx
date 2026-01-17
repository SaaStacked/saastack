import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import FormPage from '../../../framework/components/form/FormPage.tsx';

export function HomeAuthenticatedPage() {
  const { t: translate } = useTranslation();
  return (
    <FormPage>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 w-full max-w-2xl">
        <OptionCard navigateTo="/cars/search">
          <img
            src="/images/car-icon.svg"
            width={256}
            height={256}
            alt={translate('pages.home.home_authenticated.links.search_cars')}
            className="dark:invert"
          />
          <span className="mt-2 text-2xl">{translate('pages.home.home_authenticated.links.search_cars')}</span>
        </OptionCard>

        <OptionCard navigateTo="/bookings/reserve">
          <img
            src="/images/booking-icon.svg"
            width={256}
            height={256}
            alt={translate('pages.home.home_authenticated.links.reserve_car')}
            className="dark:invert"
          />
          <span className="mt-2 text-2xl">{translate('pages.home.home_authenticated.links.reserve_car')}</span>
        </OptionCard>
      </div>
    </FormPage>
  );
}

const OptionCard: React.FC<{
  children: React.ReactNode;
  navigateTo: string;
}> = ({ children, navigateTo }) => (
  <Link
    className="rounded-lg border-2 shadow-xl border-neutral-200 dark:border-neutral-600 hover:border-neutral-300 dark:hover:border-neutral-500 hover:bg-neutral-200 dark:hover:bg-neutral-700 transition-all duration-150 p-4 flex flex-col items-center space-y-4"
    to={navigateTo}
  >
    {children}
  </Link>
);
