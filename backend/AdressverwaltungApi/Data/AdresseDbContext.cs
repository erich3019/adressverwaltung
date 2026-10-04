using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Models;

namespace AdressverwaltungApi.Data;

public class AdresseDbContext : DbContext
{
    // IHttpContextAccessor ermöglicht Zugriff auf den aktuellen Benutzer im HTTP-Request
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AdresseDbContext(
        DbContextOptions<AdresseDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Adresse>   Adressen { get; set; }
    public DbSet<City>      Cities   { get; set; }
    public DbSet<User>      Users    { get; set; }
    public DbSet<Settings>  Settings { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Automatisches Befüllen der Audit-Felder bei INSERT und UPDATE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Audit-Felder für alle geänderten AuditableEntity-Einträge setzen.
    /// Wird von SaveChanges() und SaveChangesAsync() aufgerufen.
    /// </summary>
    private void ApplyAuditFields()
    {
        // DateTime.UtcNow hat Kind=UTC, aber die Spalte ist 'timestamp without time zone'.
        // Npgsql 6+ lehnt Kind=UTC für diesen Typ ab → Kind=Unspecified setzen.
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        // Benutzername ermitteln:
        // 1. Aus dem JWT-Claim (Name) des angemeldeten Benutzers
        // 2. Fallback: "system" (z.B. beim Seeding oder Hintergrundprozessen)
        // Frei setzbare Request-Header werden bewusst nicht berücksichtigt,
        // weil sich sonst die Audit-Felder fälschen liessen.
        var claimUser   = _httpContextAccessor?.HttpContext?.User?.Identity?.Name;
        var currentUser = !string.IsNullOrWhiteSpace(claimUser) ? claimUser : "system";

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreateDate = now;
                    entry.Entity.CreatedBy  = currentUser;

                    // DateFrom auf heute setzen, falls nicht explizit übergeben
                    if (entry.Entity.DateFrom == default)
                        entry.Entity.DateFrom = DateOnly.FromDateTime(now);
                    break;

                case EntityState.Modified:
                    entry.Entity.ChangeDate = now;
                    entry.Entity.ChangedBy  = currentUser;

                    // CreateDate und CreatedBy niemals überschreiben!
                    entry.Property(e => e.CreateDate).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified  = false;
                    break;
            }
        }
    }

    // Synchrone Variante (z.B. beim Seeding in Program.cs)
    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    // Asynchrone Variante (Standard-Pfad in allen Controllern)
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Gemeinsame Audit-Spalten für alle AuditableEntity-Typen ─────────
        // EF Core vererbt die Felder automatisch durch Vererbung.
        // Wir konfigurieren sie explizit für maximale Kontrolle.

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType, b =>
                {
                    b.Property<DateTime>("CreateDate")
                        .IsRequired()
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<DateTime?>("ChangeDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string?>("ChangedBy")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<DateOnly>("DateFrom")
                        .IsRequired()
                        .HasColumnType("date");

                    b.Property<DateOnly?>("DateTo")
                        .HasColumnType("date");
                });
            }
        }

        // B-12: Pflichtfelder und Längen stehen als DataAnnotations an den Modellen (B-07).
        // Hier folgt nur, was sich dort nicht ausdrücken lässt: Tabellennamen und Indizes.
        modelBuilder.Entity<Adresse>().ToTable("Adressen");

        modelBuilder.Entity<City>(entity =>
        {
            entity.ToTable("Cities");

            // Index für schnelle PLZ-Suche
            entity.HasIndex(c => c.PostalCode);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            // E-Mail muss eindeutig sein
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Settings>().ToTable("Settings");
    }
}
