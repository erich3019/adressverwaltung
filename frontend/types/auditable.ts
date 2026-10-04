/**
 * Audit-Felder und Gültigkeitszeitraum – geerbt von allen Backend-Entitäten.
 * Die Felder werden automatisch vom Backend befüllt:
 *   - createDate / createdBy  → beim ersten Speichern (POST)
 *   - changeDate / changedBy  → bei jeder Änderung (PATCH/PUT)
 *
 * Das Frontend übergibt diese Felder NICHT beim Erstellen/Ändern –
 * sie werden rein zur Anzeige (read-only) verwendet.
 *
 * Datentypen: ISO 8601 Strings (wie von OData / JSON geliefert)
 *   - createDate / changeDate → "2024-06-01T12:30:00Z"  (DateTime UTC)
 *   - dateFrom / dateTo       → "2024-06-01"             (DateOnly / date)
 */
export interface AuditFields {
  /** Zeitstempel der Erstellung (UTC). Automatisch gesetzt vom Backend. */
  createDate: string;

  /** Benutzername, der den Datensatz erstellt hat. */
  createdBy: string;

  /** Zeitstempel der letzten Änderung (UTC). null bis zur ersten Änderung. */
  changeDate: string | null;

  /** Benutzername, der zuletzt geändert hat. null bis zur ersten Änderung. */
  changedBy: string | null;

  /** Gültig ab (ISO-Datum, z.B. "2024-01-01"). */
  dateFrom: string;

  /** Gültig bis (ISO-Datum). null = unbegrenzt gültig. */
  dateTo: string | null;
}

/** Felder, die das Backend selbst vergibt und die der Client nie sendet. */
type ServerFields = 'id' | 'createDate' | 'createdBy' | 'changeDate' | 'changedBy';

/**
 * F-08: Typ für das Erstellen einer Entität.
 * id und Audit-Felder entfallen; der Gültigkeitszeitraum ist optional
 * (dateFrom: Standard heute, dateTo: Standard null = unbegrenzt gültig).
 */
export type CreateOf<T extends AuditFields> = Omit<T, ServerFields | 'dateFrom' | 'dateTo'> & {
  dateFrom?: string;
  dateTo?: string | null;
};

/** F-08: Typ für das Aktualisieren (alle Felder optional, Server-Felder ausgeschlossen). */
export type UpdateOf<T extends AuditFields> = Partial<Omit<T, ServerFields>>;
