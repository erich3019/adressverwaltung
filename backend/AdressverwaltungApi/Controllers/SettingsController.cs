using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Dtos;
using AdressverwaltungApi.Models;

namespace AdressverwaltungApi.Controllers;

/// <summary>
/// Controller für Anwendungseinstellungen.
/// GET /settings   – aktuelle Einstellungen lesen
/// PUT /settings   – Benachrichtigungs-E-Mail und Akzentfarbe ändern (nur Rolle Admin)
/// </summary>
[Authorize]
[Route("settings")]
[ApiController]
public class SettingsController : ControllerBase
{
    private readonly AdresseDbContext _context;

    public SettingsController(AdresseDbContext context)
    {
        _context = context;
    }

    // GET /settings
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _context.Settings.FirstOrDefaultAsync();

        return Ok(ToResponse(settings));
    }

    // PUT /settings – nur für Administratoren
    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] SettingsUpdateRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Leer ist erlaubt (keine Benachrichtigung); sonst muss es eine E-Mail-Adresse sein
        var email = req.NotificationEmail?.Trim() ?? string.Empty;
        if (email.Length > 0 && !new EmailAddressAttribute().IsValid(email))
        {
            return BadRequest(new { message = "Ungültige E-Mail-Adresse." });
        }

        if (req.AccentColor is not null && !AccentColors.IsValid(req.AccentColor))
        {
            return BadRequest(new { message = $"Unbekannte Farbe. Erlaubt sind: {string.Join(", ", AccentColors.All)}." });
        }

        var settings = await _context.Settings.FirstOrDefaultAsync();

        if (settings is null)
        {
            // Einstellungen beim ersten Aufruf erstellen
            settings = new Settings();
            _context.Settings.Add(settings);
        }

        settings.NotificationEmail = email;
        if (req.AccentColor is not null)
        {
            settings.AccentColor = req.AccentColor;
        }
        await _context.SaveChangesAsync();

        return Ok(ToResponse(settings));
    }

    // Die Rolle stammt aus der Datenbank (TokenUserValidator), nicht aus dem Token
    private SettingsResponse ToResponse(Settings? settings) => new(
        settings?.NotificationEmail ?? string.Empty,
        settings?.AccentColor ?? AccentColors.Default,
        CanEdit: User.IsInRole(Roles.Admin));
}
