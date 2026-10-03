namespace AdressverwaltungApi.Models;

/// <summary>
/// Abstrakte Basisklasse für alle Entitäten mit Audit-Feldern und Gültigkeitszeitraum.
///
/// Audit-Felder werden automatisch im DbContext befüllt:
///   - CreateDate / CreatedBy  → beim ersten Speichern (INSERT)
///   - ChangeDate / ChangedBy  → bei jeder Änderung (UPDATE)
///
/// Gültigkeitszeitraum:
///   - DateFrom → Pflichtfeld, standardmässig wird das heutige Datum gesetzt
///   - DateTo   → optional (null = unbegrenzt gültig)
/// </summary>
public abstract class AuditableEntity
{
    // ─────────────────────────────────────────────────────────────────────────
    // Audit: Erstellungsinformationen
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Zeitstempel der Erstellung (UTC), automatisch gesetzt.</summary>
    public DateTime CreateDate { get; set; }

    /// <summary>Benutzername, der den Datensatz erstellt hat. Automatisch aus dem HTTP-Kontext ermittelt.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    // ─────────────────────────────────────────────────────────────────────────
    // Audit: Änderungsinformationen
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Zeitstempel der letzten Änderung (UTC), wird bei jedem UPDATE gesetzt.</summary>
    public DateTime? ChangeDate { get; set; }

    /// <summary>Benutzername, der zuletzt geändert hat. Wird bei jedem UPDATE gesetzt.</summary>
    public string? ChangedBy { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Gültigkeitszeitraum
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gültig ab (inklusive). Standardmässig wird das heutige Datum gesetzt,
    /// falls beim Erstellen kein Wert übergeben wird.
    /// </summary>
    public DateOnly DateFrom { get; set; }

    /// <summary>
    /// Gültig bis (inklusive). null bedeutet "unbegrenzt gültig".
    /// </summary>
    public DateOnly? DateTo { get; set; }
}
