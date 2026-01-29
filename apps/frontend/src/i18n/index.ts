import { createI18n } from 'vue-i18n';
import de from './locales/de';
import en from './locales/en';

// Detect browser language
function detectLocale(): string {
  const stored = localStorage.getItem('kalkimo_locale');
  if (stored && ['de', 'en'].includes(stored)) {
    return stored;
  }

  const browserLang = navigator.language.split('-')[0];
  return ['de', 'en'].includes(browserLang) ? browserLang : 'de';
}

export const i18n = createI18n({
  legacy: false, // Use Composition API mode
  locale: detectLocale(),
  fallbackLocale: 'de',
  messages: {
    de,
    en
  },
  numberFormats: {
    de: {
      currency: {
        style: 'currency',
        currency: 'EUR',
        notation: 'standard'
      },
      decimal: {
        style: 'decimal',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
      },
      percent: {
        style: 'percent',
        minimumFractionDigits: 1,
        maximumFractionDigits: 2
      },
      integer: {
        style: 'decimal',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
      }
    },
    en: {
      currency: {
        style: 'currency',
        currency: 'EUR',
        notation: 'standard'
      },
      decimal: {
        style: 'decimal',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
      },
      percent: {
        style: 'percent',
        minimumFractionDigits: 1,
        maximumFractionDigits: 2
      },
      integer: {
        style: 'decimal',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
      }
    }
  },
  datetimeFormats: {
    de: {
      short: {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
      },
      long: {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      },
      yearMonth: {
        year: 'numeric',
        month: 'long'
      }
    },
    en: {
      short: {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
      },
      long: {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      },
      yearMonth: {
        year: 'numeric',
        month: 'long'
      }
    }
  }
});

// Helper to change locale
export function setLocale(locale: 'de' | 'en') {
  i18n.global.locale.value = locale;
  localStorage.setItem('kalkimo_locale', locale);
  document.documentElement.setAttribute('lang', locale);
}

// Helper to get current locale
export function getLocale(): string {
  return i18n.global.locale.value;
}

export default i18n;
