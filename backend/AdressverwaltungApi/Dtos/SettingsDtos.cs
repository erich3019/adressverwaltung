using System.ComponentModel.DataAnnotations;

namespace AdressverwaltungApi.Dtos;

/// <summary>
/// Request-Body für PUT /settings – enthält nur die änderbaren Felder.
/// Eine leere E-Mail-Adresse schaltet die Benachrichtigung aus; ohne AccentColor
/// bleibt die gespeicherte Farbe unverändert.
/// </summary>
public record SettingsUpdateRequest(
    [MaxLength(256)] string? NotificationEmail,
    [MaxLength(20)] string? AccentColor = null
);

/// <summary>
/// Antwort-Body für GET und PUT /settings.
/// CanEdit sagt dem Frontend, ob der angemeldete Benutzer die Einstellungen ändern darf.
/// </summary>
public record SettingsResponse(string NotificationEmail, string AccentColor, bool CanEdit);
