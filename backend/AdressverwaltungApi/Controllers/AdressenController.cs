using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Services;

namespace AdressverwaltungApi.Controllers;

/// <summary>
/// OData-Controller für die Adresse-Entität.
/// Erbt die CRUD-Logik von ODataCrudController (DRY, B-08), hält die E-Mail-Adresse
/// eindeutig und löst nach dem Erstellen die Benachrichtigung via INotificationService
/// aus (SRP, B-03).
/// </summary>
public class AdressenController : ODataCrudController<Adresse>
{
    protected override DbSet<Adresse> Entities    => _context.Adressen;
    protected override string EntityDisplayName   => "Adresse";

    private const string EmailVergebenMessage = "Diese E-Mail-Adresse ist bereits bei einer anderen Adresse erfasst.";

    private readonly INotificationService _notificationService;

    public AdressenController(
        AdresseDbContext context,
        INotificationService notificationService) : base(context)
    {
        _notificationService = notificationService;
    }

    // Die E-Mail-Adresse ist eindeutig, ohne Rücksicht auf Gross- und Kleinschreibung:
    // gespeichert wird sie deshalb wie bei den Benutzern in Kleinbuchstaben.
    protected override async Task<string?> OnSavingAsync(Adresse adresse)
    {
        if (adresse.Email is null)
            return null;

        adresse.Email = adresse.Email.Trim().ToLowerInvariant();

        var vergeben = await _context.Adressen
            .AnyAsync(a => a.Email == adresse.Email && a.Id != adresse.Id);

        return vergeben ? EmailVergebenMessage : null;
    }

    protected override Task OnCreatedAsync(Adresse adresse)
        => _notificationService.NotifyNewAdresseAsync(adresse);
}
