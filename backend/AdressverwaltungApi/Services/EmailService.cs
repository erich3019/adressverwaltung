using System.Net;
using System.Net.Mail;

namespace AdressverwaltungApi.Services;

/// <summary>
/// SMTP-Implementierung des E-Mail-Dienstes.
/// Konfiguration erfolgt über appsettings.json (SmtpSettings-Abschnitt).
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        // Konfiguration aus appsettings.json lesen
        var smtpHost     = _config["SmtpSettings:Host"]        ?? throw new InvalidOperationException("SmtpSettings:Host fehlt.");
        // B-04: int.TryParse verhindert FormatException bei ungültigem Konfigurationswert
        if (!int.TryParse(_config["SmtpSettings:Port"], out var smtpPort))
            smtpPort = 587;
        var smtpUser     = _config["SmtpSettings:Username"]    ?? throw new InvalidOperationException("SmtpSettings:Username fehlt.");
        var smtpPass     = _config["SmtpSettings:Password"]    ?? throw new InvalidOperationException("SmtpSettings:Password fehlt.");
        // B-05: Schlüssel vereinheitlicht auf FromAddress (war SenderEmail, docker-compose nutzt FromAddress)
        var senderEmail  = _config["SmtpSettings:FromAddress"] ?? smtpUser;
        var senderName   = _config["SmtpSettings:SenderName"]  ?? "Adressverwaltung";
        var enableSsl    = bool.Parse(_config["SmtpSettings:EnableSsl"] ?? "true");

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl   = enableSsl,
        };

        using var message = new MailMessage(
            from:    new MailAddress(senderEmail, senderName),
            to:      new MailAddress(to)
        )
        {
            Subject    = subject,
            Body       = body,
            IsBodyHtml = false,
        };

        await client.SendMailAsync(message);
        _logger.LogInformation("E-Mail gesendet an {To}: {Subject}", to, subject);
    }
}
