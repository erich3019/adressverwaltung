import { AuditFields, CreateOf, UpdateOf } from './auditable';

/**
 * Repräsentiert eine vollständige Adresse (wie in der Datenbank gespeichert).
 * Enthält alle Audit-Felder via AuditFields (read-only, vom Backend gesetzt).
 */
export interface Adresse extends AuditFields {
  id: number;
  vorname: string;
  name: string;
  strasse: string;
  strassennummer: string;
  plz: string;
  ort: string;
}

/** Typ für das Erstellen einer neuen Adresse (siehe CreateOf). */
export type AdresseCreate = CreateOf<Adresse>;

/** Typ für das Aktualisieren einer Adresse (siehe UpdateOf). */
export type AdresseUpdate = UpdateOf<Adresse>;
