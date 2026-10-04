using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Dtos;
using AdressverwaltungApi.Models;

namespace AdressverwaltungApi.Controllers;

/// <summary>
/// Controller für Registrierung und Login.
/// Sicherheitshinweise:
/// - Passwörter werden mit ASP.NET Core PasswordHasher gehasht (PBKDF2 + Salt)
/// - Das PasswordHash-Feld wird NIEMALS in Responses zurückgegeben
/// - Fehlermeldungen sind bewusst generisch (kein Hinweis ob E-Mail oder Passwort falsch)
/// - Bei erfolgreichem Login wird ein JWT Bearer-Token ausgestellt (8 h Gültigkeit)
/// - Registrierung ist nur für bereits angemeldete Benutzer möglich
/// </summary>
[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AdresseDbContext _context;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IConfiguration _config;

    // Fester Hash für die Passwortprüfung bei unbekannter E-Mail. Einmalig berechnet,
    // damit Login für bekannte und unbekannte Benutzer gleich lange dauert.
    private static readonly User DummyUser = new();
    private static readonly string DummyHash =
        new PasswordHasher<User>().HashPassword(DummyUser, Guid.NewGuid().ToString());

    public AuthController(
        AdresseDbContext context,
        IPasswordHasher<User> hasher,
        IConfiguration config)
    {
        _context = context;
        _hasher  = hasher;
        _config  = config;
    }

    // ---------------------------------------------------------------
    // JWT-Token erstellen
    // ---------------------------------------------------------------
    private string GenerateJwt(User user)
    {
        var key   = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var hours = double.Parse(_config["Jwt:ExpiresInHours"] ?? "8");

        // Claims = Informationen, die im Token gespeichert werden
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(hours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private const string RegisterMessage =
        "Falls diese E-Mail noch nicht registriert ist, wurde ein Konto erstellt.";

    // POST /auth/register
    // Nur mit gültigem JWT: Neue Benutzer erhalten vollen Zugriff auf alle Daten,
    // deshalb darf sich niemand anonym selbst registrieren.
    [Authorize]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // E-Mail case-insensitiv prüfen (B-06: ToUpper() statt ToLower() – culture-safe in EF Core)
        var emailNormalized = req.Email.Trim().ToUpperInvariant();
        var exists = await _context.Users.AnyAsync(u =>
            u.Email.ToUpper() == emailNormalized);

        if (exists)
        {
            // Gleiche Meldung wie bei Erfolg → kein User-Enumeration-Angriff möglich
            return Ok(new { message = RegisterMessage });
        }

        var user = new User
        {
            Email       = req.Email.Trim().ToLowerInvariant(),
            DisplayName = req.DisplayName.Trim(),
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = RegisterMessage });
    }

    // POST /auth/login
    // Gibt { id, name, email } zurück – das Format, das NextAuth.js erwartet
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Benutzer anhand E-Mail suchen (B-06: culture-safe via ToUpper)
        var loginEmailNormalized = req.Email.Trim().ToUpperInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email.ToUpper() == loginEmailNormalized);

        // Passwort prüfen – bei ungültigem Benutzer trotzdem "prüfen"
        // (verhindert Timing-Angriffe durch gleiche Ausführungszeit)
        var hashToVerify = user?.PasswordHash ?? DummyHash;
        var verifyTarget = user ?? DummyUser;
        var result       = _hasher.VerifyHashedPassword(verifyTarget, hashToVerify, req.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            // Bewusst generische Fehlermeldung
            return Unauthorized(new { message = "Ungültige Anmeldedaten." });
        }

        // JWT-Token ausstellen und zusammen mit den Benutzerdaten zurückgeben.
        // Das Frontend speichert den Token und sendet ihn bei jedem API-Aufruf mit.
        var token = GenerateJwt(user);

        return Ok(new LoginResponse(
            Id:    user.Id.ToString(),
            Name:  user.DisplayName,
            Email: user.Email,
            Token: token
        ));
    }
}
