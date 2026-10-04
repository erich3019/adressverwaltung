namespace AdressverwaltungApi.Options;

/// <summary>Typisierte Konfiguration des Abschnitts "SmtpSettings" (B-09).</summary>
public class SmtpOptions
{
    public const string SectionName = "SmtpSettings";

    public string  Host        { get; set; } = string.Empty;
    public int     Port        { get; set; } = 587;
    public string  Username    { get; set; } = string.Empty;
    public string  Password    { get; set; } = string.Empty;
    /// <summary>Absenderadresse; ohne Angabe wird der SMTP-Benutzer verwendet.</summary>
    public string? FromAddress { get; set; }
    public string  SenderName  { get; set; } = "Adressverwaltung";
    public bool    EnableSsl   { get; set; } = true;
}
