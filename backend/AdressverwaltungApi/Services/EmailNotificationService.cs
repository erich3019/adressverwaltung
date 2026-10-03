using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Models;

namespace AdressverwaltungApi.Services;

/// <summary>
/// E-Mail-basierte Implementierung von INotificationService.
/// Ausgelagert aus AdressenController gemäss SRP:
/// Controller sind für HTTP-Verarbeitung zuständig, nicht für Notification-Logik.
/// </summary>
public class EmailNotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly AdresseDbContext _context;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IEmailService emailService,
        AdresseDbContext context,
        ILogger<EmailNotificationService> logger)
    {
        _emailService = emailService;
        _context      = context;
        _logger       = logger;
    }

    public async Task NotifyNewAdresseAsync(Adresse adresse)
    {
        try
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.NotificationEmail))
            {
                _logger.LogInformation(
                    "Keine Benachrichtigungs-E-Mail konfiguriert. Adresse {Id} gespeichert.", adresse.Id);
                return;
            }

            var betreff = $"Neue Adresse erfasst: {adresse.Vorname} {adresse.Name}";
            var inhalt  = $"""
                Neue Adresse wurde erfasst:

                Name:    {adresse.Vorname} {adresse.Name}
                Strasse: {adresse.Strasse} {adresse.Strassennummer}
                PLZ/Ort: {adresse.Plz} {adresse.Ort}
                """;

            await _emailService.SendAsync(settings.NotificationEmail, betreff, inhalt);
        }
        catch (Exception ex)
        {
            // Fehler beim E-Mail-Versand blockiert NICHT die API-Antwort
            _logger.LogError(ex, "Fehler beim E-Mail-Versand für Adresse {Id}.", adresse.Id);
        }
    }
}
