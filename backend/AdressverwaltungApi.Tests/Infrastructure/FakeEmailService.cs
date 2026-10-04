using System.Collections.Concurrent;
using AdressverwaltungApi.Services;

namespace AdressverwaltungApi.Tests.Infrastructure;

/// <summary>
/// Ersetzt den SMTP-Versand in Tests: E-Mails werden nur aufgezeichnet.
/// Über <see cref="FailWith"/> lässt sich ein Versandfehler simulieren.
/// </summary>
public class FakeEmailService : IEmailService
{
    public record SentEmail(string To, string Subject, string Body);

    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent.ToArray();

    public Exception? FailWith { get; set; }

    public Task SendAsync(string to, string subject, string body)
    {
        if (FailWith is not null)
            throw FailWith;

        _sent.Enqueue(new SentEmail(to, subject, body));
        return Task.CompletedTask;
    }

    public void Reset()
    {
        _sent.Clear();
        FailWith = null;
    }
}
