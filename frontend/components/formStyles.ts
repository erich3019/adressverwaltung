// F-09: Gemeinsame Tailwind-Klassen für Formularfelder.
// Vorher in AdresseForm, CityForm, LoginForm und der Einstellungsseite je einzeln definiert.
export const inputClass =
  'w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-hidden ' +
  'focus:ring-2 focus:ring-akzent-500 focus:border-transparent bg-white';

export const labelClass = 'block text-sm font-medium text-gray-700 mb-1';

export const primaryButtonClass =
  'bg-akzent-600 text-white px-6 py-2 rounded-lg hover:bg-akzent-700 font-medium ' +
  'transition-colors disabled:opacity-50 disabled:cursor-not-allowed';
