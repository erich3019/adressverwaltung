namespace AdressverwaltungApi.Services;

/// <summary>
/// Zählt fehlgeschlagene Anmeldungen pro E-Mail-Adresse und sperrt die Adresse
/// vorübergehend, wenn es zu viele werden (Schutz vor Passwort-Raten).
/// </summary>
public interface ILoginThrottle
{
    /// <summary>true, solange für diese E-Mail-Adresse keine Anmeldung möglich ist.</summary>
    bool IsBlocked(string email);

    void RegisterFailure(string email);

    void Reset(string email);
}
