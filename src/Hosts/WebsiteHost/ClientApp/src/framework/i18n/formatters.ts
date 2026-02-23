import { useCurrentUser } from '../providers/CurrentUserContext.tsx';
import i18n from './index';

const getLocaleAndTimezone = (): { locale: string; timezone: string } => {
  const defaultLocale = i18n.language || 'en-NZ';
  const defaultTimezone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'Pacific/Auckland';

  try {
    const { profile } = useCurrentUser();
    return {
      locale: profile?.locale || defaultLocale,
      timezone: profile?.timezone || defaultTimezone
    };
  } catch {
    // Context not available, fall back to defaults
    return {
      locale: defaultLocale,
      timezone: defaultTimezone
    };
  }
};

export const useFormatters = () => {
  const { locale, timezone } = getLocaleAndTimezone();

  return {
    formatCurrency: (amount: number | null | undefined, currency: string = 'NZD'): string => {
      if (amount === null || amount === undefined) {
        return '-';
      }

      return new Intl.NumberFormat(locale, {
        style: 'currency',
        currency,
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
      }).format(amount);
    },

    formatDate: (date: Date | null | undefined, options?: Intl.DateTimeFormatOptions): string => {
      if (!date) {
        return '-';
      }

      const defaultOptions: Intl.DateTimeFormatOptions = {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        timeZone: timezone
      };

      return new Intl.DateTimeFormat(locale, { ...defaultOptions, ...options }).format(date);
    },

    formatTime: (date: Date | null | undefined, options?: Intl.DateTimeFormatOptions): string => {
      if (!date) {
        return '-';
      }

      const defaultOptions: Intl.DateTimeFormatOptions = {
        hour: '2-digit',
        minute: '2-digit',
        timeZone: timezone
      };

      return new Intl.DateTimeFormat(locale, { ...defaultOptions, ...options }).format(date);
    },

    formatDateTime: (date: Date | null | undefined): string => {
      if (!date) {
        return '-';
      }

      return new Intl.DateTimeFormat(locale, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        timeZone: timezone
      }).format(date);
    },

    calculateDaysInProgress: (startDate: Date | null | undefined): number | null => {
      if (!startDate) {
        return null;
      }

      return Math.floor((Date.now() - +startDate) / (1000 * 60 * 60 * 24));
    }
  };
};
