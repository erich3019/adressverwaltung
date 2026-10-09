using System.Security.Claims;
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
/// - Wiederholte Fehlversuche sperren die E-Mail-Adresse vorübergehend (ILoginThrottle)
/// - Registrierung ist nur für Administratoren möglich
/// - Die Abmeldung widerruft die ausgestellten Tokens des Benutzers
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
    private readonly ILoginThrottle _throttle;

    public AuthController(
        AdresseDbContext context,
        IPasswordHasher<User> hasher,
        ITokenService tokenService,
        ILoginThrottle throttle)
    {
        _context      = context;
        _hasher       = hasher;
        _tokenService = tokenService;
        _throttle     = throttle;
    }

    // POST /auth/register
    // Nur für Administratoren: Neue Benutzer erhalten Zugriff auf alle Adressen,
    // deshalb darf sich niemand selbst registrieren.
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var role = string.IsNullOrWhiteSpace(req.Role) ? Roles.User : req.Role.Trim();
        if (!Roles.IsValid(role))
        {
            return BadRequest(new { message = $"Unbekannte Rolle. Erlaubt sind {Roles.Admin} und {Roles.User}." });
        }

        // Gleiche Meldung für neue und bestehende E-Mail → kein User-Enumeration-Angriff möglich
        if (await FindUserByEmailAsync(req.Email) is null)
        {
            var user = new User
            {
                Email       = req.Email.Trim().ToLowerInvariant(),
                DisplayName = req.DisplayName.Trim(),
                Role        = role,
            };
            user.PasswordHash = _hasher.HashPassword(user, req.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = RegisterMessage });
    }

    // POST /auth/login
    // Gibt { id, name, email, token } zurück – das Format, das NextAuth.js erwartet
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Nach zu vielen Fehlversuchen ist die Adresse vorübergehend gesperrt –
        // auch für das richtige Passwort und auch für unbekannte Adressen.
        if (_throttle.IsBlocked(req.Email))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Zu viele Anmeldeversuche. Bitte später erneut versuchen." });
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
            _throttle.RegisterFailure(req.Email);

            // Bewusst generische Fehlermeldung
            return Unauthorized(new { message = "Ungültige Anmeldedaten." });
        }

        _throttle.Reset(req.Email);

        return Ok(new LoginResponse(
            Id:    user.Id.ToString(),
            Name:  user.DisplayName,
            Email: user.Email,
            Role:  user.Role,
            Token: _tokenService.CreateToken(user)
        ));
    }

    // POST /auth/logout
    // Widerruft alle Tokens des angemeldeten Benutzers (auch die anderer Geräte):
    // Die Token-Version wird erhöht, ältere Tokens weist TokenUserValidator danach ab.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1));

        return NoContent();
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
