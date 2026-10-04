import { AuditFields, CreateOf, UpdateOf } from './auditable';

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

/** Typ für das Erstellen einer neuen Stadt (siehe CreateOf). */
export type CityCreate = CreateOf<City>;

/** Typ für das Aktualisieren einer Stadt (siehe UpdateOf). */
export type CityUpdate = UpdateOf<City>;
