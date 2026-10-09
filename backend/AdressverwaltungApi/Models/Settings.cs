using System.ComponentModel.DataAnnotations;

namespace AdressverwaltungApi.Models;

/// <summary>
/// Anwendungseinstellungen (wird als einzelne Zeile in der Datenbank gespeichert).
/// Enthält Audit-Felder und Gültigkeitszeitraum via AuditableEntity.
/// </summary>
public class Settings : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// E-Mail-Adresse, an die bei einer neuen Adresse eine Benachrichtigung gesendet wird.
    /// Leer bedeutet: keine Benachrichtigung.
    /// </summary>
    [Required(AllowEmptyStrings = true)]
    [MaxLength(256)]
    public string NotificationEmail { get; set; } = string.Empty;

    /// <summary>Akzentfarbe der Oberfläche, siehe <see cref="AccentColors"/>.</summary>
    [Required]
    [MaxLength(20)]
    public string AccentColor { get; set; } = AccentColors.Default;
}
