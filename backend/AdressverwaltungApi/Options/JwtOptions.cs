namespace AdressverwaltungApi.Options;

/// <summary>
/// Typisierte Konfiguration des Abschnitts "Jwt" (B-09).
/// Ersetzt die verstreuten Zugriffe über Zeichenketten wie config["Jwt:Key"].
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 braucht mindestens 256 Bit, also 32 Zeichen.</summary>
    public const int MinKeyLength = 32;

    public string Key            { get; set; } = string.Empty;
    public string Issuer         { get; set; } = string.Empty;
    public string Audience       { get; set; } = string.Empty;
    public double ExpiresInHours { get; set; } = 8;
}
