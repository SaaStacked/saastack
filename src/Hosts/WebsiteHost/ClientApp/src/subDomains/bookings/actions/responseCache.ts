import { TenantedCacheKey } from '../../../framework/constants.ts';

const bookingCacheKeys = {
  all: [TenantedCacheKey, 'bookings'],
  booking: {
    query: (bookingId: string) => [...bookingCacheKeys.all, bookingId],
    mutate: (bookingId: string) => [bookingCacheKeys.booking.query(bookingId)]
  }
};

export default bookingCacheKeys;
