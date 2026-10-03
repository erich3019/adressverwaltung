import { AuditFields } from './auditable';

/**
 * Repräsentiert eine Stadt mit Postleitzahl.
 * Enthält alle Audit-Felder via AuditFields (read-only, vom Backend gesetzt).
 * Feldbezeichnungen auf Englisch (postalCode, cityName) gemäss Backend-Konvention.
 */
export interface City extends AuditFields {
  id: number;
  postalCode: string;
  cityName: string;
}

/**
 * Typ für das Erstellen einer neuen Stadt.
 * Audit-Felder und id werden weggelassen – das Backend setzt sie automatisch.
 */
export type CityCreate = Omit<City,
  'id' | 'createDate' | 'createdBy' | 'changeDate' | 'changedBy' | 'dateFrom' | 'dateTo'
> & {
  dateFrom?: string;  // optional – Backend setzt Standard: heute
  dateTo?: string | null;
};

/** Typ für das Aktualisieren (alle Felder optional, Audit-Felder ausgeschlossen). */
export type CityUpdate = Partial<Omit<City,
  'id' | 'createDate' | 'createdBy' | 'changeDate' | 'changedBy'
>>;
