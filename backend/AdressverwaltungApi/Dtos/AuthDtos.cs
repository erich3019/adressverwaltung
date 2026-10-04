using System.ComponentModel.DataAnnotations;

namespace AdressverwaltungApi.Dtos;

/// <summary>Request-Body für POST /auth/register</summary>
public record RegisterRequest(
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MinLength(8)][MaxLength(128)] string Password,
    [Required][MaxLength(100)] string DisplayName
);

/// <summary>Request-Body für POST /auth/login</summary>
public record LoginRequest(
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MaxLength(128)] string Password
);

/// <summary>
/// Antwort-Body für erfolgreichen POST /auth/login.
/// Enthält den JWT Bearer-Token, den das Frontend bei jedem API-Aufruf
/// im Authorization-Header mitsendet.
/// </summary>
public record LoginResponse(
    string Id,
    string Name,
    string Email,
    string Token
);
