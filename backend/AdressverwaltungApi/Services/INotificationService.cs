using AdressverwaltungApi.Models;

namespace AdressverwaltungApi.Services;

/// <summary>
/// Abstraktes Interface für Benachrichtigungen.
/// Ermöglicht den Austausch der Implementierung (E-Mail, SMS, Webhook)
/// ohne den aufrufenden Controller zu ändern (Open/Closed Principle).
/// </summary>
public interface INotificationService
{
    Task NotifyNewAdresseAsync(Adresse adresse);

    /// <summary>Teilt einem neu angelegten Benutzer mit, dass es seinen Zugang gibt.</summary>
    Task NotifyNewUserAsync(User user);
}
