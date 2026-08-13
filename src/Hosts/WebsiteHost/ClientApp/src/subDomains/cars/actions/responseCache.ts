import { TenantedCacheKey } from '../../../framework/constants.ts';

const carCacheKeys = {
  all: [TenantedCacheKey, 'cars'],
  car: {
    query: (carId: string) => [...carCacheKeys.all, carId],
    mutate: (carId: string) => [carCacheKeys.car.query(carId)]
  }
};

export default carCacheKeys;
