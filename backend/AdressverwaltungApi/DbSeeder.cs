using System.Security.Cryptography;
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
    /// Ohne Seed:AdminPassword wird ein zufälliges Passwort erzeugt und einmalig geloggt –
    /// es gibt bewusst kein fest einprogrammiertes Standardpasswort.
    /// </summary>
    public static void Seed(
        AdresseDbContext db,
        IPasswordHasher<User> hasher,
        IConfiguration config,
        ILogger logger)
    {
        // Nur seeden, wenn die Tabelle leer ist
        if (db.Users.Any()) return;

        var email    = config["Seed:AdminEmail"]    ?? "admin@example.com";
        var password = config["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
            logger.LogWarning(
                "Seed:AdminPassword ist nicht gesetzt. Admin-Benutzer {Email} wurde mit dem zufälligen Passwort {Password} angelegt. Bitte nach dem ersten Login ändern.",
                email, password);
        }

        var admin = new User
        {
            Email       = email,
            DisplayName = "Admin",
            Role        = Roles.Admin,
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);

        db.Users.Add(admin);
        db.SaveChanges();
    }
}
