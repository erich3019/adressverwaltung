using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Services;

namespace AdressverwaltungApi.Controllers;

/// <summary>
/// OData-Controller für die Adresse-Entität.
/// Erbt die CRUD-Logik von ODataCrudController (DRY, B-08) und löst nach dem
/// Erstellen die Benachrichtigung via INotificationService aus (SRP, B-03).
/// </summary>
public class AdressenController : ODataCrudController<Adresse>
{
    protected override DbSet<Adresse> Entities    => _context.Adressen;
    protected override string EntityDisplayName   => "Adresse";

    private readonly INotificationService _notificationService;

    public AdressenController(
        AdresseDbContext context,
        INotificationService notificationService) : base(context)
    {
        _notificationService = notificationService;
    }

    protected override Task OnCreatedAsync(Adresse adresse)
        => _notificationService.NotifyNewAdresseAsync(adresse);
}
