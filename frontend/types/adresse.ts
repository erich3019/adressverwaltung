import { AuditFields } from './auditable';

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

/**
 * Typ für das Erstellen einer neuen Adresse.
 * Audit-Felder und id werden weggelassen – das Backend setzt sie automatisch.
 * dateFrom kann optional übergeben werden (Standard: heute).
 * dateTo ist optional (Standard: null = unbegrenzt gültig).
 */
export type AdresseCreate = Omit<Adresse,
  'id' | 'createDate' | 'createdBy' | 'changeDate' | 'changedBy' | 'dateFrom' | 'dateTo'
> & {
  dateFrom?: string;  // optional – Backend setzt Standard: heute
  dateTo?: string | null;
};

/** Typ für das Aktualisieren (alle Felder optional, Audit-Felder ausgeschlossen). */
export type AdresseUpdate = Partial<Omit<Adresse,
  'id' | 'createDate' | 'createdBy' | 'changeDate' | 'changedBy'
>>;
