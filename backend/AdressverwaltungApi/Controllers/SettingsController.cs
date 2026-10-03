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
/// PUT /settings   – Benachrichtigungs-E-Mail ändern
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

        if (settings is null)
        {
            return Ok(new { notificationEmail = "" });
        }

        return Ok(new { notificationEmail = settings.NotificationEmail });
    }

    // PUT /settings
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
            settings = new Settings { NotificationEmail = req.NotificationEmail.Trim() };
            _context.Settings.Add(settings);
        }
        else
        {
            settings.NotificationEmail = req.NotificationEmail.Trim();
        }

        await _context.SaveChangesAsync();

        return Ok(new { notificationEmail = settings.NotificationEmail });
    }
}
