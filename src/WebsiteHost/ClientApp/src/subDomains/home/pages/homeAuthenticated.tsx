import { useTranslation } from 'react-i18next';
import Button from '../../../framework/components/button/Button.tsx';


export function HomeAuthenticatedPage() {
  const { t: translate } = useTranslation();
  return (
    <div className="container min-h-screen flex items-center justify-center">
      <div className="flex flex-col items-center space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 w-full max-w-2xl">
          <Button className="rounded-full flex-col py-8" variant="outline" navigateTo="/cars/search">
            <img
              src="/images/car-icon.svg"
              width={256}
              height={256}
              alt={translate('pages.home.home_authenticated.links.search_cars')}
            />
            <span className="mt-2 text-2xl">{translate('pages.home.home_authenticated.links.search_cars')}</span>
          </Button>

          <Button className="rounded-full flex-col py-8" variant="outline" navigateTo="/bookings/reserve">
            <img
              src="/images/booking-icon.svg"
              width={256}
              height={256}
              alt={translate('pages.home.home_authenticated.links.reserve_car')}
            />
            <span className="mt-2 text-2xl">{translate('pages.home.home_authenticated.links.reserve_car')}</span>
          </Button>
        </div>
      </div>
    </div>
  );
}
