using System.ComponentModel.DataAnnotations;

namespace AdressverwaltungApi.Models;

/// <summary>
/// Adress-Entität mit Audit-Feldern und Gültigkeitszeitraum.
/// Audit-Felder werden automatisch im DbContext befüllt.
/// B-07: DataAnnotations direkt am Modell deklariert (Clean Code: Constraints
/// gehören dorthin, wo der Typ definiert ist – nicht nur in OnModelCreating).
/// </summary>
public class Adresse : AuditableEntity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Vorname { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Strasse { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Strassennummer { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Plz { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Ort { get; set; } = string.Empty;
}
