using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AdressverwaltungApi.Controllers;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AdressverwaltungApi.Tests;

/// <summary>Rollen, Widerruf von Tokens und Seitengrösse der Listen.</summary>
[Collection(ApiCollection.Name)]
public class RolesAndRevocationTests : ODataTestBase
{
    public RolesAndRevocationTests(ApiFactory factory) : base(factory) { }

    private static object NeuerBenutzer(string email, string? role = null) => new
    {
        email,
        password    = "Ein-Sicheres-Passwort-1!",
        displayName = "Neuer Benutzer",
        role,
    };

    [Fact]
    public async Task User_DarfAdressenPflegen_AberWederBenutzerAnlegenNochEinstellungenAendern()
    {
        using var user = await Factory.CreateClientForNewUserAsync(Roles.User);
        var email = $"von-user-{Guid.NewGuid():N}@example.com";

        var adresse       = await user.PostAsJsonAsync("/odata/Adressen", NeueAdresse());
        var lesen         = await user.GetAsync("/settings");
        var registrierung = await user.PostAsJsonAsync("/auth/register", NeuerBenutzer(email));
        var einstellungen = await user.PutAsJsonAsync("/settings", new { notificationEmail = "x@example.com" });

        Assert.Equal(HttpStatusCode.Created, adresse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, lesen.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, registrierung.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, einstellungen.StatusCode);
        await Factory.WithDbContextAsync(async db =>
            Assert.False(await db.Users.AnyAsync(u => u.Email == email)));
    }

    [Theory]
    [InlineData(null, Roles.User)]
    [InlineData(Roles.Admin, Roles.Admin)]
    public async Task Register_VergibtDieGewuenschteRolle_OhneAngabeUser(string? gewuenscht, string erwartet)
    {
        var email = $"rolle-{Guid.NewGuid():N}@example.com";

        var response = await Client.PostAsJsonAsync("/auth/register", NeuerBenutzer(email, gewuenscht));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Factory.WithDbContextAsync(async db =>
            Assert.Equal(erwartet, (await db.Users.SingleAsync(u => u.Email == email)).Role));
    }

    [Fact]
    public async Task Register_MitUnbekannterRolle_Liefert400()
    {
        var response = await Client.PostAsJsonAsync(
            "/auth/register", NeuerBenutzer($"rolle-{Guid.NewGuid():N}@example.com", "Superuser"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rollenwechsel_GiltSofort_OhneNeuesToken()
    {
        using var user = await Factory.CreateClientForNewUserAsync(Roles.Admin);
        var vorher = await user.PutAsJsonAsync("/settings", new { notificationEmail = "a@example.com" });

        // Alle weiteren Admins dieses Tests zu "User" machen (der gemeinsame Testbenutzer bleibt Admin)
        await Factory.WithDbContextAsync(db => db.Users
            .Where(u => u.Email.StartsWith("admin-"))
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, Roles.User)));
        var nachher = await user.PutAsJsonAsync("/settings", new { notificationEmail = "b@example.com" });

        Assert.Equal(HttpStatusCode.OK, vorher.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, nachher.StatusCode);
    }

    [Fact]
    public async Task Logout_WiderruftDasToken_NeueAnmeldungFunktioniert()
    {
        using var user = await Factory.CreateClientForNewUserAsync(Roles.User);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/odata/Adressen")).StatusCode);

        var logout = await user.PostAsync("/auth/logout", null);
        var danach = await user.GetAsync("/odata/Adressen");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, danach.StatusCode);

        // Der gemeinsame Testbenutzer ist davon nicht betroffen
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/odata/Adressen")).StatusCode);
    }

    [Fact]
    public async Task Token_EinesGeloeschtenBenutzers_WirdAbgewiesen()
    {
        using var user = await Factory.CreateClientForNewUserAsync(Roles.User);

        await Factory.WithDbContextAsync(db => db.Users
            .Where(u => u.Email.StartsWith("user-"))
            .ExecuteDeleteAsync());
        var response = await user.GetAsync("/odata/Adressen");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_LiefertDieRolle()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email    = ApiFactory.TestUserEmail,
            password = ApiFactory.TestUserPassword,
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Roles.Admin, body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Liste_OhneTop_LiefertHoechstensEineSeite_UndEinenFolgelink()
    {
        var anzahl = ODataCrudController<City>.MaxPageSize + 5;
        await Factory.WithDbContextAsync(async db =>
        {
            db.Cities.AddRange(Enumerable.Range(0, anzahl)
                .Select(i => new City { PostalCode = (1000 + i).ToString(), CityName = $"Ort {i}" }));
            await db.SaveChangesAsync();
        });

        var body = await GetJsonAsync("/odata/Cities?$count=true");

        Assert.Equal(ODataCrudController<City>.MaxPageSize, body.GetProperty("value").GetArrayLength());
        Assert.Equal(anzahl, body.GetProperty("@odata.count").GetInt32());
        Assert.True(body.TryGetProperty("@odata.nextLink", out _));
    }
}
