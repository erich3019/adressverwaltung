using Microsoft.Extensions.Caching.Memory;

namespace AdressverwaltungApi.Services;

/// <summary>
/// ILoginThrottle im Arbeitsspeicher: Nach MaxFailures Fehlversuchen innerhalb des
/// Zeitfensters ist die E-Mail-Adresse bis zu dessen Ablauf gesperrt.
/// Gezählt wird pro Adresse, unabhängig davon, ob es dazu einen Benutzer gibt –
/// die Sperre verrät also nicht, welche Konten existieren.
/// Der Zähler geht bei einem Neustart verloren; für eine einzelne Instanz genügt das.
/// </summary>
public sealed class MemoryLoginThrottle : ILoginThrottle, IDisposable
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    // Obergrenze, damit sich der Speicher nicht mit erfundenen Adressen füllen lässt
    private const int MaxTrackedEmails = 10_000;

    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaxTrackedEmails });

    private sealed class Counter { public int Failures; }

    public bool IsBlocked(string email)
        => _cache.TryGetValue(Key(email), out Counter? counter)
           && Volatile.Read(ref counter!.Failures) >= MaxFailures;

    public void RegisterFailure(string email)
    {
        var counter = _cache.GetOrCreate(Key(email), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Window;
            entry.Size = 1;
            return new Counter();
        });

        Interlocked.Increment(ref counter!.Failures);
    }

    public void Reset(string email) => _cache.Remove(Key(email));

    public void Dispose() => _cache.Dispose();

    private static string Key(string email) => email.Trim().ToUpperInvariant();
}
