using System.ComponentModel.DataAnnotations;

namespace AdressverwaltungApi.Models;

/// <summary>
/// Benutzer für E-Mail/Passwort-Login.
/// Das Passwort wird als Hash gespeichert (niemals im Klartext).
/// Enthält Audit-Felder und Gültigkeitszeitraum via AuditableEntity.
/// </summary>
public class User : AuditableEntity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Passwort-Hash (generiert via ASP.NET Core PasswordHasher).
    /// Wird niemals im Klartext gespeichert oder zurückgegeben.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Anzeigename des Benutzers (z.B. "Anna Meier")</summary>
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;
}
