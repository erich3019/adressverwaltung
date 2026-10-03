using System.ComponentModel.DataAnnotations;

namespace AdressverwaltungApi.Dtos;

/// <summary>Request-Body für PUT /settings – enthält nur das änderbare Feld</summary>
public record SettingsUpdateRequest(
    [Required][EmailAddress][MaxLength(256)] string NotificationEmail
);
