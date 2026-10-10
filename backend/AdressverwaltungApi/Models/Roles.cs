namespace AdressverwaltungApi.Models;

/// <summary>
/// Rollen eines Benutzers. "User" pflegt Adressen und Städte; "Admin" darf zusätzlich
/// Benutzer verwalten und die Einstellungen ändern.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User  = "User";

    public static bool IsValid(string? role) => role is Admin or User;
}
