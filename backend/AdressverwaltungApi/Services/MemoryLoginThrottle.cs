using Microsoft.Extensions.Caching.Memory;

namespace AdressverwaltungApi.Services;

/// <summary>
/// ILoginThrottle im Arbeitsspeicher: Nach MaxFailures Fehlversuchen innerhalb des
/// Zeitfensters ist die E-Mail-Adresse für LockDuration gesperrt, gerechnet ab dem
/// letzten dieser Fehlversuche.
/// Gezählt wird pro Adresse, unabhängig davon, ob es dazu einen Benutzer gibt –
/// die Sperre verrät also nicht, welche Konten existieren.
/// Zähler und Sperren gehen bei einem Neustart verloren. Für bestehende Benutzer hält
/// AuthController die Sperre zusätzlich als Sperrkennzeichen in der Datenbank fest.
/// </summary>
public sealed class MemoryLoginThrottle : ILoginThrottle, IDisposable
{
    public const int MaxFailures = 3;
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

    // Fehlversuche zählen zusammen, solange sie innerhalb dieses Fensters liegen
    public static readonly TimeSpan Window = LockDuration;

    // Obergrenze, damit sich der Speicher nicht mit erfundenen Adressen füllen lässt
    private const int MaxTrackedEmails = 10_000;

    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaxTrackedEmails });

    private sealed class Counter
    {
        public int Failures;
        public DateTime? BlockedUntil;
    }

    public bool IsBlocked(string email)
    {
        if (!_cache.TryGetValue(Key(email), out Counter? counter)) return false;

        lock (counter!)
        {
            return counter.BlockedUntil > DateTime.UtcNow;
        }
    }

    public DateTime? RegisterFailure(string email)
    {
        var key     = Key(email);
        var counter = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Window;
            entry.Size = 1;
            return new Counter();
        });

        lock (counter!)
        {
            counter.Failures++;
            if (counter.Failures != MaxFailures) return null;

            // Die Sperre beginnt jetzt: Eintrag mit neuer Ablaufzeit ablegen
            counter.BlockedUntil = DateTime.UtcNow.Add(LockDuration);
            _cache.Set(key, counter, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = LockDuration,
                Size = 1,
            });

            return counter.BlockedUntil;
        }
    }

    public void Reset(string email) => _cache.Remove(Key(email));

    public void Dispose() => _cache.Dispose();

    private static string Key(string email) => email.Trim().ToUpperInvariant();
}
