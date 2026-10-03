namespace AdressverwaltungApi.Services;

/// <summary>
/// Schnittstelle für den E-Mail-Versand.
/// Ermöglicht einfaches Austauschen der Implementierung (z.B. SMTP → SendGrid).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sendet eine E-Mail asynchron.
    /// </summary>
    /// <param name="to">Empfänger-E-Mail-Adresse</param>
    /// <param name="subject">Betreff</param>
    /// <param name="body">Inhalt (plain text)</param>
    Task SendAsync(string to, string subject, string body);
}
