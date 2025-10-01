import { useTranslation } from 'react-i18next';
import Button from '../../../framework/components/button/Button.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';


export function HomeAuthenticatedPage() {
  const { t: translate } = useTranslation();
  return (
    <FormPage>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 w-full max-w-2xl">
        <Button className="rounded-full flex-col py-8" variant="outline" navigateTo="/cars/search">
          <img
            src="/images/car-icon.svg"
            width={256}
            height={256}
            alt={translate('pages.home.home_authenticated.links.search_cars')}
            className="dark:invert"
          />
          <span className="mt-2 text-2xl">{translate('pages.home.home_authenticated.links.search_cars')}</span>
        </Button>

        <Button className="rounded-full flex-col py-8" variant="outline" navigateTo="/bookings/reserve">
          <img
            src="/images/booking-icon.svg"
            width={256}
            height={256}
            alt={translate('pages.home.home_authenticated.links.reserve_car')}
            className="dark:invert"
          />
          <span className="mt-2 text-2xl">{translate('pages.home.home_authenticated.links.reserve_car')}</span>
        </Button>
      </div>
    </FormPage>
  );
}
