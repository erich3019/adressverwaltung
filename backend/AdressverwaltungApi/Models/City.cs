using System.ComponentModel.DataAnnotations;

namespace AdressverwaltungApi.Models;

/// <summary>
/// Repräsentiert eine Stadt mit Postleitzahl.
/// Feldbezeichnungen auf Englisch gemäss Konvention.
/// Enthält Audit-Felder und Gültigkeitszeitraum via AuditableEntity.
/// </summary>
public class City : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>Postleitzahl (z.B. "8001")</summary>
    [Required]
    [MaxLength(10)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Ortsname (z.B. "Zürich")</summary>
    [Required]
    [MaxLength(100)]
    public string CityName { get; set; } = string.Empty;
}
