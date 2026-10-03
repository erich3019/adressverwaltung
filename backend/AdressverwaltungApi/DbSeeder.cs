using Microsoft.AspNetCore.Identity;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Models;

namespace AdressverwaltungApi;

/// <summary>
/// Befüllt die Datenbank mit initialen Daten (Admin-Benutzer).
/// Ausgelagert aus Program.cs gemäss Single Responsibility Principle (SRP):
/// Program.cs ist nur für Konfiguration und Middleware zuständig.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Legt den Admin-Benutzer an, falls noch kein Benutzer vorhanden ist.
    /// Zugangsdaten werden aus der Konfiguration gelesen (Seed-Abschnitt).
    /// </summary>
    public static void Seed(
        AdresseDbContext db,
        IPasswordHasher<User> hasher,
        IConfiguration config)
    {
        // Nur seeden, wenn die Tabelle leer ist
        if (db.Users.Any()) return;

        var email    = config["Seed:AdminEmail"]    ?? "admin@example.com";
        var password = config["Seed:AdminPassword"] ?? "Test-1234!";

        var admin = new User
        {
            Email       = email,
            DisplayName = "Admin",
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);

        db.Users.Add(admin);
        db.SaveChanges();
    }
}
