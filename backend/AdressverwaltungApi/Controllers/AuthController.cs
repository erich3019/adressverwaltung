using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Dtos;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Services;

namespace AdressverwaltungApi.Controllers;

/// <summary>
/// Controller für Registrierung und Login.
/// Sicherheitshinweise:
/// - Passwörter werden mit ASP.NET Core PasswordHasher gehasht (PBKDF2 + Salt)
/// - Das PasswordHash-Feld wird NIEMALS in Responses zurückgegeben
/// - Fehlermeldungen sind bewusst generisch (kein Hinweis ob E-Mail oder Passwort falsch)
/// - Bei erfolgreichem Login wird ein JWT Bearer-Token ausgestellt (ITokenService)
/// - Registrierung ist nur für bereits angemeldete Benutzer möglich
/// </summary>
[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private const string RegisterMessage =
        "Falls diese E-Mail noch nicht registriert ist, wurde ein Konto erstellt.";

    // Fester Hash für die Passwortprüfung bei unbekannter E-Mail. Einmalig berechnet,
    // damit Login für bekannte und unbekannte Benutzer gleich lange dauert.
    private static readonly User DummyUser = new();
    private static readonly string DummyHash =
        new PasswordHasher<User>().HashPassword(DummyUser, Guid.NewGuid().ToString());

    private readonly AdresseDbContext _context;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ITokenService _tokenService;

    public AuthController(
        AdresseDbContext context,
        IPasswordHasher<User> hasher,
        ITokenService tokenService)
    {
        _context      = context;
        _hasher       = hasher;
        _tokenService = tokenService;
    }

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

        // Gleiche Meldung für neue und bestehende E-Mail → kein User-Enumeration-Angriff möglich
        if (await FindUserByEmailAsync(req.Email) is null)
        {
            var user = new User
            {
                Email       = req.Email.Trim().ToLowerInvariant(),
                DisplayName = req.DisplayName.Trim(),
            };
            user.PasswordHash = _hasher.HashPassword(user, req.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = RegisterMessage });
    }

    // POST /auth/login
    // Gibt { id, name, email, token } zurück – das Format, das NextAuth.js erwartet
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await FindUserByEmailAsync(req.Email);

        // Passwort prüfen – bei ungültigem Benutzer trotzdem "prüfen"
        // (verhindert Timing-Angriffe durch gleiche Ausführungszeit)
        var result = _hasher.VerifyHashedPassword(
            user ?? DummyUser,
            user?.PasswordHash ?? DummyHash,
            req.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            // Bewusst generische Fehlermeldung
            return Unauthorized(new { message = "Ungültige Anmeldedaten." });
        }

        return Ok(new LoginResponse(
            Id:    user.Id.ToString(),
            Name:  user.DisplayName,
            Email: user.Email,
            Token: _tokenService.CreateToken(user)
        ));
    }

    /// <summary>
    /// Sucht einen Benutzer unabhängig von Gross-/Kleinschreibung der E-Mail
    /// (B-06: ToUpper() statt ToLower() – culture-safe in EF Core).
    /// </summary>
    private Task<User?> FindUserByEmailAsync(string email)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return _context.Users.FirstOrDefaultAsync(u => u.Email.ToUpper() == normalized);
    }
}
