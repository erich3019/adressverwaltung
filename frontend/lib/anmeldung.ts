// Anmeldesperre: Das Backend sperrt eine E-Mail-Adresse nach drei Fehlversuchen für
// fünf Minuten (MemoryLoginThrottle) und antwortet dann mit 429.

// Fehlercode, mit dem authorize() in lib/auth.ts die Sperre an das Login-Formular meldet
export const ANMELDUNG_GESPERRT = 'AnmeldungGesperrt';

// Dauer der Sperre für die Meldung im Login-Formular; massgebend ist das Backend
export const SPERRE_MINUTEN = 5;
