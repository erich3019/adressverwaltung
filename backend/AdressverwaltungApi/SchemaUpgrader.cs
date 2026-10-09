using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;

namespace AdressverwaltungApi;

/// <summary>
/// Zieht Spalten nach, die es in einer bestehenden Datenbank noch nicht gibt.
/// Nötig, weil EnsureCreated() eine vorhandene Datenbank nicht verändert.
/// Jede Anweisung lässt sich beliebig oft ausführen.
/// </summary>
public static class SchemaUpgrader
{
    public static void Apply(AdresseDbContext db)
    {
        // Rollen: Benutzer, die es vor der Einführung schon gab, hatten vollen Zugriff
        // und behalten ihn als "Admin". Danach gilt für neue Zeilen der Standard "User".
        db.Database.ExecuteSqlRaw("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name = 'Users' AND column_name = 'Role') THEN
                    ALTER TABLE "Users" ADD COLUMN "Role" character varying(20) NOT NULL DEFAULT 'Admin';
                    ALTER TABLE "Users" ALTER COLUMN "Role" SET DEFAULT 'User';
                END IF;
            END $$;
            """);

        // Token-Widerruf: Zähler, der bei der Abmeldung erhöht wird
        db.Database.ExecuteSqlRaw("""
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "TokenVersion" integer NOT NULL DEFAULT 0;
            """);
    }
}
