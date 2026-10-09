using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AdressverwaltungApi.Services;
using AdressverwaltungApi.Tests.Infrastructure;

namespace AdressverwaltungApi.Tests;

/// <summary>Härtung der API: Over-Posting, Fehlerdetails, Caching und Anmeldesperre.</summary>
[Collection(ApiCollection.Name)]
public class HardeningTests : ODataTestBase
{
    public HardeningTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Post_MitIdUndAenderungsangaben_VerwirftBeides()
    {
        var city = await CreateAsync("Cities", new
        {
            id         = 5000,
            postalCode = "8001",
            cityName   = "Zürich",
            changedBy  = "gefaelscht",
            changeDate = "2001-01-01T00:00:00Z",
        });

        Assert.NotEqual(5000, city.GetProperty("id").GetInt32());
        Assert.Equal(JsonValueKind.Null, city.GetProperty("changedBy").ValueKind);
        Assert.Equal(JsonValueKind.Null, city.GetProperty("changeDate").ValueKind);
    }

    [Fact]
    public async Task Patch_MitId_AendertDenSchluesselNicht()
    {
        var city = await CreateAsync("Cities", new { postalCode = "6340", cityName = "Baar" });
        var id   = city.GetProperty("id").GetInt32();

        var response = await Client.PatchAsJsonAsync($"/odata/Cities({id})", new { id = id + 1000, cityName = "Zug" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var danach = await GetJsonAsync($"/odata/Cities({id})");
        Assert.Equal("Zug", danach.GetProperty("cityName").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/odata/Cities({id + 1000})")).StatusCode);
    }

    [Fact]
    public async Task UngueltigeAbfrage_LiefertKeinenStacktrace()
    {
        var response = await Client.GetAsync("/odata/Cities?$top=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"message\"", text);
        Assert.DoesNotContain("innererror", text);
        Assert.DoesNotContain("stacktrace", text);
        Assert.DoesNotContain("ODataException", text);
    }

    [Fact]
    public async Task Antworten_SindNichtZwischenspeicherbar()
    {
        var response = await Client.GetAsync("/odata/Adressen");

        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Login_NachZuVielenFehlversuchen_IstAuchMitRichtigemPasswortGesperrt()
    {
        var email    = $"sperre-{Guid.NewGuid():N}@example.com";
        var password = "Ein-Sicheres-Passwort-1!";
        var register = await Client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Sperre" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        using var client = Factory.CreateClient();
        for (var i = 0; i < MemoryLoginThrottle.MaxFailures; i++)
        {
            var fehlversuch = await client.PostAsJsonAsync("/auth/login", new { email, password = "falsch" });
            Assert.Equal(HttpStatusCode.Unauthorized, fehlversuch.StatusCode);
        }

        // Gross-/Kleinschreibung der Adresse umgeht die Sperre nicht
        var gesperrt = await client.PostAsJsonAsync("/auth/login", new { email = email.ToUpperInvariant(), password });

        Assert.Equal(HttpStatusCode.TooManyRequests, gesperrt.StatusCode);
    }

    [Fact]
    public async Task Login_ErfolgreicheAnmeldung_SetztDenFehlerzaehlerZurueck()
    {
        var email    = $"zaehler-{Guid.NewGuid():N}@example.com";
        var password = "Ein-Sicheres-Passwort-1!";
        await Client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Zähler" });

        using var client = Factory.CreateClient();
        for (var runde = 0; runde < 2; runde++)
        {
            for (var i = 0; i < MemoryLoginThrottle.MaxFailures - 1; i++)
                await client.PostAsJsonAsync("/auth/login", new { email, password = "falsch" });

            var erfolg = await client.PostAsJsonAsync("/auth/login", new { email, password });
            Assert.Equal(HttpStatusCode.OK, erfolg.StatusCode);
        }
    }
}
