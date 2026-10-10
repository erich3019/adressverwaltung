namespace AdressverwaltungApi.Services;

/// <summary>
/// Zählt fehlgeschlagene Anmeldungen pro E-Mail-Adresse und sperrt die Adresse
/// vorübergehend, wenn es zu viele werden (Schutz vor Passwort-Raten).
/// </summary>
public interface ILoginThrottle
{
    /// <summary>true, solange für diese E-Mail-Adresse keine Anmeldung möglich ist.</summary>
    bool IsBlocked(string email);

    /// <summary>
    /// Zählt einen Fehlversuch. Löst genau dieser Versuch die Sperre aus, wird ihr
    /// Ende (UTC) zurückgegeben, sonst null.
    /// </summary>
    DateTime? RegisterFailure(string email);

    /// <summary>Löscht Fehlerzähler und Sperre der Adresse.</summary>
    void Reset(string email);
}
