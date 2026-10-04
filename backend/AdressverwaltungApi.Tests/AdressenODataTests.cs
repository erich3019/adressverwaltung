using System.Net;
using System.Net.Http.Json;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Tests.Infrastructure;

namespace AdressverwaltungApi.Tests;

[Collection(ApiCollection.Name)]
public class AdressenODataTests : ODataTestBase
{
    public AdressenODataTests(ApiFactory factory) : base(factory) { }

    // ── GET (Collection) ────────────────────────────────────────────────────

    [Fact]
    public async Task Get_OhneDaten_LiefertLeereListe()
    {
        var adressen = await GetListAsync("/odata/Adressen");

        Assert.Empty(adressen);
    }

    [Fact]
    public async Task Get_LiefertAlleAdressen()
    {
        await CreateAsync("Adressen", NeueAdresse(name: "Meier"));
        await CreateAsync("Adressen", NeueAdresse(name: "Huber"));

        var adressen = await GetListAsync("/odata/Adressen");

        Assert.Equal(2, adressen.Count);
    }

    // ── GET (Einzeln) ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByKey_LiefertAdresse()
    {
        var erstellt = await CreateAsync("Adressen", NeueAdresse(vorname: "Anna", name: "Meier"));
        var id = erstellt.GetProperty("id").GetInt32();

        var adresse = await GetJsonAsync($"/odata/Adressen({id})");

        Assert.Equal(id, adresse.GetProperty("id").GetInt32());
        Assert.Equal("Anna", adresse.GetProperty("vorname").GetString());
        Assert.Equal("Meier", adresse.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetByKey_UnbekannteId_Liefert404()
    {
        var response = await Client.GetAsync("/odata/Adressen(999999)");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Query-Optionen ──────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_NachOrt_LiefertNurTreffer()
    {
        await CreateAsync("Adressen", NeueAdresse(name: "Meier", ort: "Bern", plz: "3000"));
        await CreateAsync("Adressen", NeueAdresse(name: "Huber", ort: "Baar"));
        await CreateAsync("Adressen", NeueAdresse(name: "Keller", ort: "Bern", plz: "3000"));

        var adressen = await GetListAsync("/odata/Adressen?$filter=ort eq 'Bern'");

        Assert.Equal(2, adressen.Count);
        Assert.All(adressen, a => Assert.Equal("Bern", a.GetProperty("ort").GetString()));
    }

    [Fact]
    public async Task Filter_MitContains_LiefertNurTreffer()
    {
        await CreateAsync("Adressen", NeueAdresse(name: "Meier"));
        await CreateAsync("Adressen", NeueAdresse(name: "Obermeier"));
        await CreateAsync("Adressen", NeueAdresse(name: "Huber"));

        var adressen = await GetListAsync("/odata/Adressen?$filter=contains(name,'eier')");

        Assert.Equal(2, adressen.Count);
    }

    [Theory]
    [InlineData("name asc", new[] { "Huber", "Keller", "Meier" })]
    [InlineData("name desc", new[] { "Meier", "Keller", "Huber" })]
    public async Task OrderBy_SortiertNachName(string orderBy, string[] erwartet)
    {
        await CreateAsync("Adressen", NeueAdresse(name: "Meier"));
        await CreateAsync("Adressen", NeueAdresse(name: "Huber"));
        await CreateAsync("Adressen", NeueAdresse(name: "Keller"));

        var adressen = await GetListAsync($"/odata/Adressen?$orderby={orderBy}");

        Assert.Equal(erwartet, adressen.Select(a => a.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task TopSkipCount_LiefertSeiteUndGesamtzahl()
    {
        foreach (var name in new[] { "A", "B", "C", "D", "E" })
            await CreateAsync("Adressen", NeueAdresse(name: name));

        var body = await GetJsonAsync("/odata/Adressen?$orderby=name&$top=2&$skip=1&$count=true");

        Assert.Equal(5, body.GetProperty("@odata.count").GetInt32());
        var namen = body.GetProperty("value").EnumerateArray()
            .Select(a => a.GetProperty("name").GetString());
        Assert.Equal(new[] { "B", "C" }, namen);
    }

    [Fact]
    public async Task Top_UeberMaximum_Liefert400()
    {
        var response = await Client.GetAsync("/odata/Adressen?$top=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Select_LiefertNurGewaehlteFelder()
    {
        await CreateAsync("Adressen", NeueAdresse());

        var adresse = (await GetListAsync("/odata/Adressen?$select=name,ort")).Single();

        Assert.True(adresse.TryGetProperty("name", out _));
        Assert.True(adresse.TryGetProperty("ort", out _));
        Assert.False(adresse.TryGetProperty("vorname", out _));
        Assert.False(adresse.TryGetProperty("strasse", out _));
    }

    [Fact]
    public async Task Filter_UnbekanntesFeld_Liefert400()
    {
        var response = await Client.GetAsync("/odata/Adressen?$filter=gibtEsNicht eq 'x'");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── POST ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ErstelltAdresse()
    {
        var erstellt = await CreateAsync("Adressen", NeueAdresse(vorname: "Anna", name: "Meier", ort: "Zug", plz: "6300"));

        Assert.True(erstellt.GetProperty("id").GetInt32() > 0);
        Assert.Equal("Anna", erstellt.GetProperty("vorname").GetString());
        Assert.Equal("Meier", erstellt.GetProperty("name").GetString());
        Assert.Equal("Dorfstrasse", erstellt.GetProperty("strasse").GetString());
        Assert.Equal("99", erstellt.GetProperty("strassennummer").GetString());
        Assert.Equal("6300", erstellt.GetProperty("plz").GetString());
        Assert.Equal("Zug", erstellt.GetProperty("ort").GetString());

        var gespeichert = await GetListAsync("/odata/Adressen");
        Assert.Single(gespeichert);
    }

    [Fact]
    public async Task Post_SetztAuditFelder()
    {
        var erstellt = await CreateAsync("Adressen", NeueAdresse());
        var id = erstellt.GetProperty("id").GetInt32();

        await Factory.WithDbContextAsync(async db =>
        {
            var adresse = await db.Adressen.FindAsync(id);
            Assert.NotNull(adresse);
            Assert.Equal(ApiFactory.TestUserName, adresse.CreatedBy);
            Assert.InRange(adresse.CreateDate, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5));
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), adresse.DateFrom);
            Assert.Null(adresse.ChangeDate);
            Assert.Null(adresse.ChangedBy);
            Assert.Null(adresse.DateTo);
        });
    }

    [Fact]
    public async Task Post_LeeresPflichtfeld_Liefert400()
    {
        var response = await Client.PostAsJsonAsync("/odata/Adressen", NeueAdresse(name: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetListAsync("/odata/Adressen"));
    }

    [Fact]
    public async Task Post_ZuLangerWert_Liefert400()
    {
        var response = await Client.PostAsJsonAsync("/odata/Adressen",
            NeueAdresse(plz: "12345678901")); // MaxLength(10)

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetListAsync("/odata/Adressen"));
    }

    // ── POST: E-Mail-Benachrichtigung ───────────────────────────────────────

    [Fact]
    public async Task Post_MitBenachrichtigungsEmail_SendetEmail()
    {
        await SetzeBenachrichtigungsEmailAsync("info@example.com");

        await CreateAsync("Adressen", NeueAdresse(vorname: "Anna", name: "Meier"));

        var email = Assert.Single(Factory.Emails.Sent);
        Assert.Equal("info@example.com", email.To);
        Assert.Contains("Anna Meier", email.Subject);
    }

    [Fact]
    public async Task Post_OhneBenachrichtigungsEmail_SendetNichts()
    {
        await CreateAsync("Adressen", NeueAdresse());

        Assert.Empty(Factory.Emails.Sent);
    }

    [Fact]
    public async Task Post_EmailVersandSchlaegtFehl_AdresseWirdTrotzdemErstellt()
    {
        await SetzeBenachrichtigungsEmailAsync("info@example.com");
        Factory.Emails.FailWith = new InvalidOperationException("SMTP nicht erreichbar");

        await CreateAsync("Adressen", NeueAdresse());

        Assert.Single(await GetListAsync("/odata/Adressen"));
    }

    // ── PATCH ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Patch_AendertNurUebergebeneFelder()
    {
        var erstellt = await CreateAsync("Adressen", NeueAdresse(vorname: "Anna", name: "Meier"));
        var id = erstellt.GetProperty("id").GetInt32();

        var response = await Client.PatchAsJsonAsync($"/odata/Adressen({id})",
            new { ort = "Winterthur", plz = "8400" });

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}");
        var adresse = await GetJsonAsync($"/odata/Adressen({id})");
        Assert.Equal("Winterthur", adresse.GetProperty("ort").GetString());
        Assert.Equal("8400", adresse.GetProperty("plz").GetString());
        Assert.Equal("Anna", adresse.GetProperty("vorname").GetString());
        Assert.Equal("Meier", adresse.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Patch_SetztChangeFelder_UndBehaeltCreateFelder()
    {
        var erstellt = await CreateAsync("Adressen", NeueAdresse());
        var id = erstellt.GetProperty("id").GetInt32();
        Adresse? vorher = null;
        await Factory.WithDbContextAsync(async db => vorher = await db.Adressen.FindAsync(id));

        var response = await Client.PatchAsJsonAsync($"/odata/Adressen({id})", new { ort = "Winterthur" });

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}");
        await Factory.WithDbContextAsync(async db =>
        {
            var nachher = await db.Adressen.FindAsync(id);
            Assert.NotNull(nachher);
            Assert.NotNull(nachher.ChangeDate);
            Assert.Equal(ApiFactory.TestUserName, nachher.ChangedBy);
            Assert.Equal(vorher!.CreateDate, nachher.CreateDate);
            Assert.Equal(vorher.CreatedBy, nachher.CreatedBy);
        });
    }

    [Fact]
    public async Task Patch_UngueltigerWert_Liefert400_UndAendertNichts()
    {
        var erstellt = await CreateAsync("Adressen", NeueAdresse(plz: "6340"));
        var id = erstellt.GetProperty("id").GetInt32();

        var response = await Client.PatchAsJsonAsync($"/odata/Adressen({id})",
            new { plz = "12345678901" }); // MaxLength(10)

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var adresse = await GetJsonAsync($"/odata/Adressen({id})");
        Assert.Equal("6340", adresse.GetProperty("plz").GetString());
    }

    [Fact]
    public async Task Patch_UnbekannteId_Liefert404()
    {
        var response = await Client.PatchAsJsonAsync("/odata/Adressen(999999)", new { ort = "Bern" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_LoeschtAdresse()
    {
        var erstellt = await CreateAsync("Adressen", NeueAdresse());
        var id = erstellt.GetProperty("id").GetInt32();

        var response = await Client.DeleteAsync($"/odata/Adressen({id})");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var danach = await Client.GetAsync($"/odata/Adressen({id})");
        Assert.Equal(HttpStatusCode.NotFound, danach.StatusCode);
    }

    [Fact]
    public async Task Delete_UnbekannteId_Liefert404()
    {
        var response = await Client.DeleteAsync("/odata/Adressen(999999)");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task SetzeBenachrichtigungsEmailAsync(string email) =>
        Factory.WithDbContextAsync(async db =>
        {
            db.Settings.Add(new Settings { NotificationEmail = email });
            await db.SaveChangesAsync();
        });
}
