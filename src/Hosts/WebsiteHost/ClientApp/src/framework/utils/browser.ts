/**
 * Gets the user's browser timezone using Intl.DateTimeFormat
 * @returns The timezone identifier (e.g., "America/New_York", "Europe/London")
 */
export function getBrowserTimezone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
  } catch (error) {
    return 'UTC';
  }
}

/**
 * Gets the user's browser locale
 * @returns The locale identifier (e.g., "en-US", "fr-FR", "de-DE")
 */
export function getBrowserLocale(): string {
  try {
    if (navigator.language) {
      return navigator.language;
    }

    if (navigator.languages && navigator.languages.length > 0) {
      return navigator.languages[0];
    }

    return 'en-US';
  } catch (error) {
    return 'en-US';
  }
}

/**
 * Gets the user's country code based on timezone and locale
 * @returns The country code (e.g., "US", "GB", "DE")
 */
export function getBrowserCountry(): string {
  try {
    // First try to extract from locale
    const locale = getBrowserLocale();
    const localeParts = locale.split('-');
    if (localeParts.length > 1) {
      return localeParts[1].toUpperCase();
    }

    // Fallback: try to infer from timezone
    const timezone = getBrowserTimezone();
    const countryFromTimezone = getCountryFromTimezone(timezone);
    if (countryFromTimezone) {
      return countryFromTimezone;
    }

    return 'US';
  } catch (error) {
    return 'US';
  }
}

/**
 * Maps common timezones to country codes
 * @param timezone The timezone identifier
 * @returns The country code or null if not found
 */
function getCountryFromTimezone(timezone: string): string | null {
  return countryTimezones[timezone] || null;
}

export const countryTimezones: Record<string, string> = {
  // North America
  'America/New_York': 'US',
  'America/Chicago': 'US',
  'America/Denver': 'US',
  'America/Los_Angeles': 'US',
  'America/Phoenix': 'US',
  'America/Anchorage': 'US',
  'America/Toronto': 'CA',
  'America/Vancouver': 'CA',
  'America/Montreal': 'CA',
  'America/Mexico_City': 'MX',

  // Europe
  'Europe/London': 'GB',
  'Europe/Dublin': 'IE',
  'Europe/Paris': 'FR',
  'Europe/Berlin': 'DE',
  'Europe/Rome': 'IT',
  'Europe/Madrid': 'ES',
  'Europe/Amsterdam': 'NL',
  'Europe/Brussels': 'BE',
  'Europe/Vienna': 'AT',
  'Europe/Zurich': 'CH',
  'Europe/Stockholm': 'SE',
  'Europe/Oslo': 'NO',
  'Europe/Copenhagen': 'DK',
  'Europe/Helsinki': 'FI',
  'Europe/Warsaw': 'PL',
  'Europe/Prague': 'CZ',
  'Europe/Budapest': 'HU',
  'Europe/Bucharest': 'RO',
  'Europe/Athens': 'GR',
  'Europe/Moscow': 'RU',

  // Asia Pacific
  'Asia/Tokyo': 'JP',
  'Asia/Seoul': 'KR',
  'Asia/Shanghai': 'CN',
  'Asia/Hong_Kong': 'HK',
  'Asia/Singapore': 'SG',
  'Asia/Bangkok': 'TH',
  'Asia/Jakarta': 'ID',
  'Asia/Manila': 'PH',
  'Asia/Kuala_Lumpur': 'MY',
  'Asia/Kolkata': 'IN',
  'Asia/Dubai': 'AE',
  'Australia/Sydney': 'AU',
  'Australia/Melbourne': 'AU',
  'Australia/Perth': 'AU',
  'Pacific/Auckland': 'NZ',

  // South America
  'America/Sao_Paulo': 'BR',
  'America/Argentina/Buenos_Aires': 'AR',
  'America/Santiago': 'CL',
  'America/Lima': 'PE',
  'America/Bogota': 'CO',

  // Africa
  'Africa/Cairo': 'EG',
  'Africa/Johannesburg': 'ZA',
  'Africa/Lagos': 'NG',
  'Africa/Casablanca': 'MA'
};
