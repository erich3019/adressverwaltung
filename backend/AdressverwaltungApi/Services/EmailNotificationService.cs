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

    // Die E-Mail geht an den neuen Benutzer selbst. Sie enthält bewusst kein Passwort:
    // E-Mails sind nicht vertraulich, das Passwort gibt der Administrator anders weiter.
    public async Task NotifyNewUserAsync(User user)
    {
        try
        {
            var betreff = "Dein Zugang zur Adressverwaltung";
            var inhalt  = $"""
                Hallo {user.DisplayName}

                Für dich wurde ein Benutzer in der Adressverwaltung angelegt.

                Anmeldung: {user.Email}
                Rolle:     {user.Role}

                Das Passwort erhältst du von der Person, die den Benutzer angelegt hat.
                """;

            await _emailService.SendAsync(user.Email, betreff, inhalt);
        }
        catch (Exception ex)
        {
            // Der Benutzer ist angelegt; ein Fehler beim Versand ändert daran nichts
            _logger.LogError(ex, "Fehler beim E-Mail-Versand für den neuen Benutzer {Id}.", user.Id);
        }
    }

    // Wie bei einer neuen Adresse geht die Meldung an die Adresse aus den Einstellungen;
    // ist dort keine hinterlegt, wird nichts gesendet.
    public async Task NotifyUserDeletedAsync(User user, string? deletedBy)
    {
        try
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.NotificationEmail))
            {
                _logger.LogInformation(
                    "Keine Benachrichtigungs-E-Mail konfiguriert. Benutzer {Id} gelöscht.", user.Id);
                return;
            }

            var betreff = $"Benutzer gelöscht: {user.DisplayName}";
            var inhalt  = $"""
                Ein Benutzer wurde gelöscht:

                Name:         {user.DisplayName}
                E-Mail:       {user.Email}
                Rolle:        {user.Role}
                Gelöscht von: {(string.IsNullOrWhiteSpace(deletedBy) ? "unbekannt" : deletedBy)}
                """;

            await _emailService.SendAsync(settings.NotificationEmail, betreff, inhalt);
        }
        catch (Exception ex)
        {
            // Der Benutzer ist gelöscht; ein Fehler beim Versand ändert daran nichts
            _logger.LogError(ex, "Fehler beim E-Mail-Versand für den gelöschten Benutzer {Id}.", user.Id);
        }
    }

    public async Task NotifyUserLockedAsync(User user)
    {
        try
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.NotificationEmail))
            {
                _logger.LogInformation(
                    "Keine Benachrichtigungs-E-Mail konfiguriert. Benutzer {Id} gesperrt.", user.Id);
                return;
            }

            var minuten = (int)MemoryLoginThrottle.LockDuration.TotalMinutes;
            var betreff = $"Benutzer gesperrt: {user.DisplayName}";
            var inhalt  = $"""
                Ein Benutzer wurde nach {MemoryLoginThrottle.MaxFailures} fehlerhaften Anmeldungen gesperrt:

                Name:   {user.DisplayName}
                E-Mail: {user.Email}
                Rolle:  {user.Role}

                Die Sperre endet nach {minuten} Minuten von selbst. Ein Administrator kann sie
                in der Benutzerverwaltung vorher aufheben.
                """;

            await _emailService.SendAsync(settings.NotificationEmail, betreff, inhalt);
        }
        catch (Exception ex)
        {
            // Die Sperre gilt; ein Fehler beim Versand ändert daran nichts
            _logger.LogError(ex, "Fehler beim E-Mail-Versand für den gesperrten Benutzer {Id}.", user.Id);
        }
    }
}
