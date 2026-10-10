using System.ComponentModel.DataAnnotations;

namespace AdressverwaltungApi.Dtos;

/// <summary>Request-Body für POST /users</summary>
public record UserCreateRequest(
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MinLength(8)][MaxLength(128)] string Password,
    [Required][MaxLength(100)] string DisplayName,
    /// <summary>"Admin" oder "User"; ohne Angabe "User".</summary>
    [MaxLength(20)] string? Role = null
);

/// <summary>
/// Request-Body für PUT /users/{id}. Die E-Mail-Adresse ist nicht änderbar;
/// ohne Password bleibt das bisherige Passwort bestehen.
/// </summary>
public record UserUpdateRequest(
    [Required][MaxLength(100)] string DisplayName,
    [Required][MaxLength(20)] string Role,
    [MinLength(8)][MaxLength(128)] string? Password = null
);

/// <summary>
/// Ein Benutzer in den Antworten von /users – ohne Passwort-Hash.
/// LockedUntil: Sperrkennzeichen des Benutzers – Ende der Anmeldesperre (UTC) oder null. IsSelf: der angemeldete Benutzer selbst.
/// </summary>
public record UserResponse(
    int Id,
    string Email,
    string DisplayName,
    string Role,
    DateTime? LockedUntil,
    bool IsSelf
);
