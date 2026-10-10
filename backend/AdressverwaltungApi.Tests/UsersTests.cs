using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Services;
using AdressverwaltungApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AdressverwaltungApi.Tests;

/// <summary>Benutzerverwaltung (/users) und die Anmeldesperre nach Fehlversuchen.</summary>
[Collection(ApiCollection.Name)]
public class UsersTests : ODataTestBase
{
    private const string Passwort = "Ein-Sicheres-Passwort-1!";

    public UsersTests(ApiFactory factory) : base(factory) { }

    private static string NeueEmail() => $"benutzer-{Guid.NewGuid():N}@example.com";

    /// <summary>Legt über POST /users einen Benutzer an und gibt ihn zurück.</summary>
    private async Task<JsonElement> ErstelleBenutzerAsync(string email, string? role = null)
    {
        var response = await Client.PostAsJsonAsync("/users",
            new { email, password = Passwort, displayName = "Neuer Benutzer", role });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password)
        => Factory.CreateClient().PostAsJsonAsync("/auth/login", new { email, password });

    /// <summary>Client mit dem Token des angegebenen Benutzers.</summary>
    private async Task<HttpClient> ClientFuerAsync(string email, string password = Passwort)
    {
        var login = await LoginAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Get_LiefertBenutzerOhnePasswortHash_UndKennzeichnetDenEigenen()
    {
        var response = await Client.GetAsync("/users");
        var text     = await response.Content.ReadAsStringAsync();
        var benutzer = JsonDocument.Parse(text).RootElement.EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("passwordHash", text, StringComparison.OrdinalIgnoreCase);
        var eigener = Assert.Single(benutzer, b => b.GetProperty("isSelf").GetBoolean());
        Assert.Equal(ApiFactory.TestUserEmail, eigener.GetProperty("email").GetString());
        Assert.Equal(Roles.Admin, eigener.GetProperty("role").GetString());
    }

    [Fact]
    public async Task User_DarfDieBenutzerverwaltungNichtAufrufen()
    {
        var ziel = await ErstelleBenutzerAsync(NeueEmail());
        var id   = ziel.GetProperty("id").GetInt32();
        using var user = await Factory.CreateClientForNewUserAsync(Roles.User);

        var liste     = await user.GetAsync("/users");
        var einzeln   = await user.GetAsync($"/users/{id}");
        var anlegen   = await user.PostAsJsonAsync("/users", new { email = NeueEmail(), password = Passwort, displayName = "X" });
        var aendern   = await user.PutAsJsonAsync($"/users/{id}", new { displayName = "X", role = Roles.Admin });
        var entsperren = await user.PostAsync($"/users/{id}/unlock", null);
        var loeschen  = await user.DeleteAsync($"/users/{id}");

        Assert.All(new[] { liste, einzeln, anlegen, aendern, entsperren, loeschen },
            r => Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode));
    }

    [Fact]
    public async Task Post_LegtBenutzerAn_DerSichAnmeldenKann()
    {
        var email = NeueEmail();

        var benutzer = await ErstelleBenutzerAsync(email.ToUpperInvariant(), Roles.Admin);
        var login    = await LoginAsync(email, Passwort);

        Assert.Equal(email, benutzer.GetProperty("email").GetString());
        Assert.Equal(Roles.Admin, benutzer.GetProperty("role").GetString());
        Assert.False(benutzer.GetProperty("isSelf").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Post_SendetDemNeuenBenutzerEineEmail_OhnePasswort()
    {
        var email = NeueEmail();

        await ErstelleBenutzerAsync(email, Roles.Admin);

        var mail = Assert.Single(Factory.Emails.Sent);
        Assert.Equal(email, mail.To);
        Assert.Contains("Neuer Benutzer", mail.Body);
        Assert.Contains(email, mail.Body);
        Assert.Contains(Roles.Admin, mail.Body);
        Assert.DoesNotContain(Passwort, mail.Body);
    }

    [Fact]
    public async Task Post_LegtBenutzerAuchAn_WennDerVersandFehlschlaegt()
    {
        var email = NeueEmail();
        Factory.Emails.FailWith = new InvalidOperationException("SMTP nicht erreichbar");

        await ErstelleBenutzerAsync(email);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, Passwort)).StatusCode);
    }

    [Fact]
    public async Task Register_SendetNurFuerEinenNeuenBenutzerEineEmail()
    {
        var email = NeueEmail();
        var body  = new { email, password = Passwort, displayName = "Registriert" };

        await Client.PostAsJsonAsync("/auth/register", body);
        await Client.PostAsJsonAsync("/auth/register", body);

        var mail = Assert.Single(Factory.Emails.Sent);
        Assert.Equal(email, mail.To);
    }

    [Fact]
    public async Task Post_MitVorhandenerEmail_Liefert409_UndSendetNichts()
    {
        var email = NeueEmail();
        await ErstelleBenutzerAsync(email);

        var response = await Client.PostAsJsonAsync("/users",
            new { email = email.ToUpperInvariant(), password = Passwort, displayName = "Doppelt" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(Factory.Emails.Sent);
    }

    [Theory]
    [InlineData("keine-adresse", Passwort, "Name", null)]
    [InlineData("a@example.com", "kurz", "Name", null)]
    [InlineData("a@example.com", Passwort, "", null)]
    [InlineData("a@example.com", Passwort, "Name", "Superuser")]
    public async Task Post_MitUngueltigenWerten_Liefert400(string email, string password, string displayName, string? role)
    {
        var response = await Client.PostAsJsonAsync("/users", new { email, password, displayName, role });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_AendertNameUndRolle_OhneDasPasswortZuAendern()
    {
        var email = NeueEmail();
        var id    = (await ErstelleBenutzerAsync(email)).GetProperty("id").GetInt32();
        using var benutzer = await ClientFuerAsync(email);

        var response = await Client.PutAsJsonAsync($"/users/{id}", new { displayName = " Anna Meier ", role = Roles.Admin });
        var geaendert = await GetJsonAsync($"/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Anna Meier", geaendert.GetProperty("displayName").GetString());
        Assert.Equal(Roles.Admin, geaendert.GetProperty("role").GetString());
        // Das Token gilt weiter, die neue Rolle sofort
        Assert.Equal(HttpStatusCode.OK, (await benutzer.GetAsync("/users")).StatusCode);
    }

    [Fact]
    public async Task Put_MitNeuemPasswort_WiderruftTokens_UndGiltFuerDieAnmeldung()
    {
        var email = NeueEmail();
        var id    = (await ErstelleBenutzerAsync(email)).GetProperty("id").GetInt32();
        using var benutzer = await ClientFuerAsync(email);
        const string neuesPasswort = "Ein-Neues-Passwort-2!";

        var response = await Client.PutAsJsonAsync($"/users/{id}",
            new { displayName = "Neuer Benutzer", role = Roles.User, password = neuesPasswort });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await benutzer.GetAsync("/odata/Adressen")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, Passwort)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, neuesPasswort)).StatusCode);
    }

    [Theory]
    [InlineData("Name", "Superuser", null)]
    [InlineData("", Roles.User, null)]
    [InlineData("Name", Roles.User, "kurz")]
    public async Task Put_MitUngueltigenWerten_Liefert400(string displayName, string role, string? password)
    {
        var id = (await ErstelleBenutzerAsync(NeueEmail())).GetProperty("id").GetInt32();

        var response = await Client.PutAsJsonAsync($"/users/{id}", new { displayName, role, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EigenerBenutzer_LaesstSichWederHerabstufenNochLoeschen()
    {
        var liste  = await GetJsonAsync("/users");
        var eigene = liste.EnumerateArray().Single(b => b.GetProperty("isSelf").GetBoolean()).GetProperty("id").GetInt32();

        var herabstufen = await Client.PutAsJsonAsync($"/users/{eigene}", new { displayName = ApiFactory.TestUserName, role = Roles.User });
        var loeschen    = await Client.DeleteAsync($"/users/{eigene}");

        Assert.Equal(HttpStatusCode.BadRequest, herabstufen.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, loeschen.StatusCode);
        await Factory.WithDbContextAsync(async db =>
            Assert.Equal(Roles.Admin, (await db.Users.SingleAsync(u => u.Id == eigene)).Role));
    }

    [Fact]
    public async Task Delete_LoeschtBenutzer_UndSeinTokenGiltNichtMehr()
    {
        var email = NeueEmail();
        var id    = (await ErstelleBenutzerAsync(email)).GetProperty("id").GetInt32();
        using var benutzer = await ClientFuerAsync(email);

        var response = await Client.DeleteAsync($"/users/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/users/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await benutzer.GetAsync("/odata/Adressen")).StatusCode);
    }

    [Fact]
    public async Task UnbekannterBenutzer_Liefert404()
    {
        const int id = int.MaxValue;

        var lesen      = await Client.GetAsync($"/users/{id}");
        var aendern    = await Client.PutAsJsonAsync($"/users/{id}", new { displayName = "X", role = Roles.User });
        var entsperren = await Client.PostAsync($"/users/{id}/unlock", null);
        var loeschen   = await Client.DeleteAsync($"/users/{id}");

        Assert.All(new[] { lesen, aendern, entsperren, loeschen },
            r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
    }

    [Fact]
    public async Task DreiFehlversuche_SperrenFuerFuenfMinuten_UndDieListeZeigtDieSperre()
    {
        Assert.Equal(3, MemoryLoginThrottle.MaxFailures);
        Assert.Equal(TimeSpan.FromMinutes(5), MemoryLoginThrottle.LockDuration);

        var email = NeueEmail();
        var id    = (await ErstelleBenutzerAsync(email)).GetProperty("id").GetInt32();

        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, "falsch")).StatusCode);
        var vorDerSperre = await GetJsonAsync($"/users/{id}");
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, "falsch")).StatusCode);
        var gesperrt = await LoginAsync(email, Passwort);
        var benutzer = await GetJsonAsync($"/users/{id}");

        Assert.Equal(JsonValueKind.Null, vorDerSperre.GetProperty("lockedUntil").ValueKind);
        Assert.Equal(HttpStatusCode.TooManyRequests, gesperrt.StatusCode);
        var bis = benutzer.GetProperty("lockedUntil").GetDateTimeOffset();
        Assert.InRange(bis - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(4.5), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Unlock_HebtDieSperreAuf()
    {
        var email = NeueEmail();
        var id    = (await ErstelleBenutzerAsync(email)).GetProperty("id").GetInt32();
        for (var i = 0; i < MemoryLoginThrottle.MaxFailures; i++)
            await LoginAsync(email, "falsch");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(email, Passwort)).StatusCode);

        var response = await Client.PostAsync($"/users/{id}/unlock", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var benutzer = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, benutzer.GetProperty("lockedUntil").ValueKind);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, Passwort)).StatusCode);
    }

    [Fact]
    public async Task OhneToken_Liefert401()
    {
        using var anonym = Factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonym.GetAsync("/users")).StatusCode);
    }
}
