using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using AdressverwaltungApi.Options;

namespace AdressverwaltungApi.Services;

/// <summary>
/// SMTP-Implementierung des E-Mail-Dienstes.
/// Konfiguration erfolgt über den Abschnitt SmtpSettings (siehe SmtpOptions).
/// </summary>
public class EmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        EnsureConfigured();

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            EnableSsl   = _options.EnableSsl,
        };

        var senderEmail = string.IsNullOrWhiteSpace(_options.FromAddress)
            ? _options.Username
            : _options.FromAddress;

        using var message = new MailMessage(
            from:    new MailAddress(senderEmail, _options.SenderName),
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

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("SmtpSettings:Host fehlt.");
        if (string.IsNullOrWhiteSpace(_options.Username))
            throw new InvalidOperationException("SmtpSettings:Username fehlt.");
        if (string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException("SmtpSettings:Password fehlt.");
    }
}
