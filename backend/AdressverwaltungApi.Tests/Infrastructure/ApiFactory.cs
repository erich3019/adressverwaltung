using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace AdressverwaltungApi.Tests.Infrastructure;

/// <summary>
/// Startet die API im Speicher (TestServer) gegen eine echte PostgreSQL-Datenbank,
/// die pro Testlauf als Wegwerf-Container (Testcontainers) hochgefahren wird.
/// Voraussetzung: Docker läuft.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestUserEmail    = "tester@example.com";
    public const string TestUserPassword = "Test-Passwort-1!";
    public const string TestUserName     = "Test User";

    // Gleiches Image wie in docker-compose.yml
    private readonly PostgreSqlContainer _database =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    private string? _token;

    public FakeEmailService Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Datenbank: Container statt ConnectionStrings:DefaultConnection
            services.RemoveAll<DbContextOptions<AdresseDbContext>>();
            services.AddDbContext<AdresseDbContext>(options =>
                options.UseNpgsql(_database.GetConnectionString()));

            // E-Mail: kein echter SMTP-Versand
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
        });
    }

    public Task InitializeAsync() => _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <summary>HTTP-Client mit gültigem JWT (über /auth/register + /auth/login bezogen).</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        _token ??= await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return client;
    }

    /// <summary>Leert alle fachlichen Tabellen, damit jeder Test bei null beginnt.</summary>
    public async Task ResetDataAsync()
    {
        await WithDbContextAsync(async db =>
        {
            await db.Adressen.ExecuteDeleteAsync();
            await db.Cities.ExecuteDeleteAsync();
            await db.Settings.ExecuteDeleteAsync();
        });
        Emails.Reset();
    }

    public async Task WithDbContextAsync(Func<AdresseDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AdresseDbContext>());
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            email       = TestUserEmail,
            password    = TestUserPassword,
            displayName = TestUserName,
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            email    = TestUserEmail,
            password = TestUserPassword,
        });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }
}

/// <summary>Alle API-Tests teilen sich eine Factory (ein Container) und laufen nacheinander.</summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
