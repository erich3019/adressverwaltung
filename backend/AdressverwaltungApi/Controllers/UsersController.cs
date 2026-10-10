using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Dtos;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Services;

namespace AdressverwaltungApi.Controllers;

/// <summary>
/// Benutzerverwaltung – nur für die Rolle Admin.
/// GET    /users              – alle Benutzer, mit Stand der Anmeldesperre
/// GET    /users/{id}         – ein Benutzer
/// POST   /users              – Benutzer anlegen
/// PUT    /users/{id}         – Anzeigename, Rolle und optional Passwort ändern
/// DELETE /users/{id}         – Benutzer löschen
/// POST   /users/{id}/unlock  – Anmeldesperre aufheben
/// Der Passwort-Hash verlässt das Backend nie. Sich selbst kann ein Administrator
/// weder löschen noch die Rolle Admin entziehen – so bleibt immer einer übrig.
/// </summary>
[Authorize(Roles = Roles.Admin)]
[Route("users")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AdresseDbContext _context;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ILoginThrottle _throttle;

    public UsersController(AdresseDbContext context, IPasswordHasher<User> hasher, ILoginThrottle throttle)
    {
        _context  = context;
        _hasher   = hasher;
        _throttle = throttle;
    }

    // GET /users
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users.AsNoTracking()
            .OrderBy(u => u.DisplayName).ThenBy(u => u.Email)
            .ToListAsync();

        return Ok(users.Select(ToResponse));
    }

    // GET /users/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

        return user is null ? NotFound() : Ok(ToResponse(user));
    }

    // POST /users
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var role = string.IsNullOrWhiteSpace(req.Role) ? Roles.User : req.Role.Trim();
        if (!Roles.IsValid(role))
        {
            return BadRequest(new { message = UnknownRoleMessage });
        }

        // Anders als /auth/register darf dieser Endpunkt sagen, dass es die Adresse
        // schon gibt: Wer ihn aufrufen darf, sieht ohnehin die ganze Benutzerliste.
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email.ToUpper() == email.ToUpperInvariant()))
        {
            return Conflict(new { message = "Für diese E-Mail-Adresse gibt es bereits einen Benutzer." });
        }

        var user = new User
        {
            Email       = email,
            DisplayName = req.DisplayName.Trim(),
            Role        = role,
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToResponse(user));
    }

    // PUT /users/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var role = req.Role.Trim();
        if (!Roles.IsValid(role))
        {
            return BadRequest(new { message = UnknownRoleMessage });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (id == CurrentUserId && role != Roles.Admin)
        {
            return BadRequest(new { message = "Die eigene Rolle Admin lässt sich nicht entziehen." });
        }

        user.DisplayName = req.DisplayName.Trim();
        user.Role        = role;

        if (!string.IsNullOrEmpty(req.Password))
        {
            user.PasswordHash = _hasher.HashPassword(user, req.Password);

            // Neues Passwort: bisherige Tokens widerrufen und eine Sperre aufheben
            user.TokenVersion++;
            _throttle.Reset(user.Email);
        }

        await _context.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    // DELETE /users/{id}
    // Tokens des gelöschten Benutzers weist TokenUserValidator danach ab.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (id == CurrentUserId)
        {
            return BadRequest(new { message = "Der eigene Benutzer lässt sich nicht löschen." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        _throttle.Reset(user.Email);

        return NoContent();
    }

    // POST /users/{id}/unlock
    [HttpPost("{id:int}/unlock")]
    public async Task<IActionResult> UnlockUser(int id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        _throttle.Reset(user.Email);

        return Ok(ToResponse(user));
    }

    private static string UnknownRoleMessage => $"Unbekannte Rolle. Erlaubt sind {Roles.Admin} und {Roles.User}.";

    private int? CurrentUserId
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private UserResponse ToResponse(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Role,
        LockedUntil: _throttle.BlockedUntil(user.Email),
        IsSelf:      user.Id == CurrentUserId);
}
