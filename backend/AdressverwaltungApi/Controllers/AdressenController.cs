using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Services;

namespace AdressverwaltungApi.Controllers;

/// <summary>
/// OData-Controller für die Adresse-Entität.
/// Erbt CRUD-Grundlogik von ODataCrudController (DRY, B-08).
/// Überschreibt Post, um die E-Mail-Benachrichtigung via INotificationService
/// auszulösen (SRP, B-03).
/// </summary>
public class AdressenController : ODataCrudController<Adresse>
{
    protected override DbSet<Adresse> Entities    => _context.Adressen;
    protected override string EntityDisplayName   => "Adresse";

    private readonly INotificationService _notificationService;
    private readonly ILogger<AdressenController> _logger;

    public AdressenController(
        AdresseDbContext context,
        INotificationService notificationService,
        ILogger<AdressenController> logger) : base(context)
    {
        _notificationService = notificationService;
        _logger              = logger;
    }

    // POST /odata/Adressen – überschrieben, um Benachrichtigung auszulösen
    public override async Task<IActionResult> Post([FromBody] Adresse adresse)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        Entities.Add(adresse);
        await _context.SaveChangesAsync();

        // E-Mail-Benachrichtigung über INotificationService (B-03: SRP)
        await _notificationService.NotifyNewAdresseAsync(adresse);

        return Created(adresse);
    }
}
