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
/// PUT /settings   – Benachrichtigungs-E-Mail ändern (nur Rolle Admin)
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

        return Ok(new SettingsResponse(settings?.NotificationEmail ?? string.Empty));
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

        var settings = await _context.Settings.FirstOrDefaultAsync();

        if (settings is null)
        {
            // Einstellungen beim ersten Aufruf erstellen
            settings = new Settings();
            _context.Settings.Add(settings);
        }

        settings.NotificationEmail = req.NotificationEmail.Trim();
        await _context.SaveChangesAsync();

        return Ok(new SettingsResponse(settings.NotificationEmail));
    }
}
