import { initReactI18next } from 'react-i18next';
import i18n from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import Backend from 'i18next-http-backend';


i18n
  .use(Backend)
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    fallbackLng: 'en',
    debug: window.isTestingOnly,
    interpolation: {
      escapeValue: false
    },
    backend: {
      loadPath: '/locales/{{lng}}/{{ns}}.json'
    },
    saveMissing: false,
    react: {
      useSuspense: false // we handle the ready state in App.tsx
    },
    detection: {
      order: ['localStorage', 'navigator', 'htmlTag'],
      lookupLocalStorage: 'i18nextLng',
      caches: ['localStorage'],
      // Map specific locales to supported languages
      convertDetectedLanguage: (lng: string) => {
        // Convert en-NZ, en-AU, en-GB, en-US, etc. to just 'en'
        if (lng.startsWith('en')) return 'en';
        return lng;
      }
    }
  });

export default i18n;
